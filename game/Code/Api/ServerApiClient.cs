using System.Threading.Tasks;

namespace Dxura.RP.Game;

public static partial class ServerApiClient
{
	private static async Task<T?> SafeApiCall<T>( Func<Dictionary<string, string>, Task<T>> apiAction,
		string errorMessage )
	{
		if ( string.IsNullOrWhiteSpace( ServerApiLink.Token ) )
		{
			if ( Networking.IsHost )
			{
				Log.Warning( $"{errorMessage}: missing server API token" );
			}
			return default;
		}

		return await ApiClientBase.SafeApiCall(
			_ => Task.FromResult( GetAuthHeaders() ),
			apiAction,
			errorMessage );
	}

	private static Dictionary<string, string> GetAuthHeaders()
	{
		return new Dictionary<string, string>
		{
			{ Constants.ApiServerTokenHeader, ServerApiLink.Token }
		};
	}

	private static bool TryQueueApiCall( Func<Dictionary<string, string>, Task> apiAction, string errorMessage )
	{
		if ( string.IsNullOrWhiteSpace( ServerApiLink.Token ) )
		{
			if ( Networking.IsHost )
			{
				Log.Warning( $"{errorMessage}: missing server API token" );
			}

			return false;
		}

		ApiClientBase.FireAndForget( async () =>
			{
				var headers = GetAuthHeaders();
				await apiAction( headers );
			},
			errorMessage );

		return true;
	}
}
