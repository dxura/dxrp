using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dxura.RP.Shared;
using Sandbox.Services;
using System.Text;

namespace Dxura.RP.Game;

/// <summary>
///     Static client for the RP API (Player)
/// </summary>
public static class PlayerApiClient
{
	// Token cache with semaphore for thread safety
	private static string? _cachedToken;
	private static DateTime _tokenExpiry = DateTime.MinValue;
	private static readonly SemaphoreSlim TokenSemaphore = new( 1, 1 );

	//
	// Core Functions
	//

	public static async Task<PlayerPulseResponseDto?> Pulse( bool afk, string hwid )
	{
		return await SafeApiCall<PlayerPulseResponseDto?>( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<PlayerPulseResponseDto>(
					$"{Constants.ApiBaseUrl}/v1/player/pulse?afk={afk}&marker={await GetMarker()}&hwid={Uri.UnescapeDataString( hwid )}", "POST",
					headers: headers );

				return response;
			},
			"Failed to pulse server as player" );
	}

	private static async Task<string> GetMarker()
	{

		const string markerFile = "sbox";

		string marker;
		if ( FileSystem.OrganizationData.FileExists( markerFile ) )
		{
			marker = await FileSystem.OrganizationData.ReadAllTextAsync( markerFile );
		}
		else
		{
			marker = Guid.NewGuid().ToString();
			FileSystem.OrganizationData.WriteAllText( markerFile, marker );
		}

		return marker;
	}

	public static async Task<bool> ShareScreenshot( byte[] imageData, bool forced = false )
	{
		await GameTask.MainThread();
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return false;
		}

		return await SafeApiCall( async headers =>
			{
				// Convert the PNG data to base64
				var base64Image = Convert.ToBase64String( imageData );

				// Send the base64 string as JSON string content
				var content = new StringContent( $"\"{base64Image}\"", Encoding.UTF8, "application/json" );

				var response = await ApiClientBase.RequestJsonAsync<bool>(
					$"{Constants.ApiBaseUrl}/v1/player/share/screenshot?forced={forced}",
					"POST",
					content,
					headers );

				return response;
			},
			"Failed to send screenshot to server" );
	}

	public static async Task<bool> Consent()
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return true;
		}

		return await SafeApiCall( async headers =>
			{
				// Do consent 
				var response = await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/player/consent",
					"POST",
					null,
					headers );

				if ( !response.IsSuccessStatusCode )
				{
					return false;
				}

				return true;
			},
			"Failed to inform server of consent" );
	}

	public static async Task<bool> Upgrade()
	{
		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<bool>(
					$"{Constants.ApiBaseUrl}/v1/player/upgrade", "POST",
					headers: headers );

				return response;
			},
			"Failed upgrade rank as player" );
	}

	//
	// Inventory Functions
	//

	public static async Task<List<InventoryItemDto>?> GetInventory()
	{
		return await SafeApiCall<List<InventoryItemDto>?>( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<List<InventoryItemDto>>(
					$"{Constants.ApiBaseUrl}/v1/player/inventory",
					"GET", headers: headers );

				return response;
			},
			"Failed to get player inventory" );
	}

	public static async Task<ItemDefinitionDto?> GetItemDefinition( Guid itemId )
	{
		return await SafeApiCall<ItemDefinitionDto?>( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<ItemDefinitionDto>(
					$"{Constants.ApiBaseUrl}/v1/player/inventory/items/{itemId}",
					"GET", headers: headers );

				return response;
			},
			$"Failed to get item definition ({itemId})" );
	}

	//
	// Common API Call Method
	//
	private static async Task<T?> SafeApiCall<T>( Func<Dictionary<string, string>, Task<T>> apiAction,
		string errorMessage )
	{
		return await ApiClientBase.SafeApiCall(
			GetAuthHeaders,
			apiAction,
			errorMessage,
			retryUnauthorized: true );
	}

	//
	// Authentication 
	//

	private static async Task<Dictionary<string, string>> GetAuthHeaders( bool forceRefresh = false )
	{
		var steamId = Connection.Local.SteamId.ToString();
		var token = await GetAuthToken( forceRefresh );

		var headers = new Dictionary<string, string>
		{
			{
				Constants.ApiSboxSteamIdHeader, steamId
			},
			{
				Constants.ApiSboxAuthTokenHeader, token
			},
			{
				Constants.ApiTenantIdHeader, ServerApiLink.Current.TenantId
			}
		};

		return headers;
	}

	private static async Task<string> GetAuthToken( bool forceRefresh = false )
	{
		// Quick check without taking semaphore
		if ( !forceRefresh && !string.IsNullOrEmpty( _cachedToken ) && DateTime.UtcNow < _tokenExpiry )
		{
			return _cachedToken;
		}

		// Token needs refresh - acquire semaphore
		await TokenSemaphore.WaitAsync();

		try
		{
			// Double-check after acquiring semaphore
			if ( !forceRefresh && !string.IsNullOrEmpty( _cachedToken ) && DateTime.UtcNow < _tokenExpiry )
			{
				return _cachedToken;
			}

			try
			{
				// Get new token
				var newToken = await Auth.GetToken( "DXRP" );

				if ( !string.IsNullOrEmpty( newToken ) )
				{
					_cachedToken = newToken;
					_tokenExpiry = DateTime.UtcNow.AddHours( 24 );
					Log.Info( "Generated new sbox auth token" );
				}

				return _cachedToken ?? "";
			}
			catch ( Exception ex )
			{
				Log.Error( $"Failed to get auth token: {ex.Message}" );
				return "";
			}
		}
		finally
		{
			TokenSemaphore.Release();
		}
	}
}
