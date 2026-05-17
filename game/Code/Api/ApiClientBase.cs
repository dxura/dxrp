using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

internal static class ApiClientBase
{
	private const int MaxQueuedRequests = 256;
	private static int _queuedRequests;

	public static async Task<T?> SafeApiCall<T>(
		Func<bool, Task<Dictionary<string, string>>> getAuthHeaders,
		Func<Dictionary<string, string>, Task<T>> apiAction,
		string errorMessage,
		bool retryUnauthorized = false )
	{
		try
		{
			var result = await GameTask.RunInThreadAsync( async () =>
			{
				var headers = await getAuthHeaders( false );
				return await apiAction( headers );
			} );

			if ( !retryUnauthorized || result is not HttpResponseMessage response )
			{
				return result;
			}

			if ( response.StatusCode != HttpStatusCode.Unauthorized )
			{
				return result;
			}

			Log.Info( "Auth token rejected (401 Unauthorized), refreshing and retrying..." );
			return await GameTask.RunInThreadAsync( async () =>
			{
				var refreshedHeaders = await getAuthHeaders( true );
				return await apiAction( refreshedHeaders );
			} );
		}
		catch ( Exception ex )
		{
			if ( retryUnauthorized && IsAuthError( ex ) )
			{
				try
				{
					Log.Info( "Auth token rejected, refreshing and retrying..." );
					return await GameTask.RunInThreadAsync( async () =>
					{
						var headers = await getAuthHeaders( true );
						return await apiAction( headers );
					} );
				}
				catch ( Exception retryEx )
				{
					Log.Warning( $"{errorMessage} after token refresh: {retryEx.Message}" );
					return default;
				}
			}

			Log.Error( $"{errorMessage}: {ex.Message}" );
			return default;
		}
	}

	public static void FireAndForget(
		Func<Task> apiAction,
		string errorMessage,
		bool logErrors = true )
	{
		var queued = Interlocked.Increment( ref _queuedRequests );
		if ( queued > MaxQueuedRequests )
		{
			Interlocked.Decrement( ref _queuedRequests );

			if ( logErrors )
			{
				Log.Warning( $"{errorMessage}: API request dropped ({MaxQueuedRequests} already queued)" );
			}

			return;
		}

		_ = GameTask.RunInThreadAsync( async () =>
		{
			try
			{
				await apiAction();
			}
			catch ( Exception ex )
			{
				if ( logErrors )
				{
					Log.Error( $"{errorMessage}: {ex.Message}" );
				}
			}
			finally
			{
				Interlocked.Decrement( ref _queuedRequests );
			}
		} );
	}

	public static async Task<T?> SafeApiCall<T>(
		Func<Task<T>> apiAction,
		string errorMessage,
		bool logErrors = true )
	{
		try
		{
			return await GameTask.RunInThreadAsync( apiAction );
		}
		catch ( Exception ex )
		{
			if ( logErrors )
			{
				Log.Error( $"{errorMessage}: {ex.Message}" );
			}

			return default;
		}
	}

	public static Task<HttpResponseMessage> RequestAsync(
		string requestUri,
		string method = "GET",
		HttpContent? content = null,
		Dictionary<string, string>? headers = null )
	{
		return Http.RequestAsync( requestUri, method, content, headers );
	}

	public static Task<T> RequestJsonAsync<T>(
		string requestUri,
		string method = "GET",
		HttpContent? content = null,
		Dictionary<string, string>? headers = null )
	{
		return Http.RequestJsonAsync<T>( requestUri, method, content, headers );
	}

	private static bool IsAuthError( Exception ex )
	{
		return ex.Message.Contains( "401" ) || ex.Message.Contains( "Unauthorized" );
	}
}
