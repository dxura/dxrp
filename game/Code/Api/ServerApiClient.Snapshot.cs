using Dxura.RP.Shared;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public static partial class ServerApiClient
{
	public static async Task<bool> SaveSnapshot( SaveSnapshotDto snapshot )
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return false;
		}

		return await SafeApiCall( async headers =>
			{
				await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/server/snapshot",
					"POST", Http.CreateJsonContent( snapshot ), headers );

				return true;
			},
			"Failed to save snapshot to API" );
	}

	public static async Task<SnapshotResponseDto?> GetSnapshot()
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return null;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<SnapshotResponseDto>(
					$"{Constants.ApiBaseUrl}/v1/server/snapshot",
					"GET", headers: headers );

				return response;
			},
			"Failed to get snapshot from API" );
	}

	public static async Task AcknowledgeServerAction( Guid actionId )
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return;
		}

		await SafeApiCall( async headers =>
			{
				await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/server/actions/{actionId}",
					"DELETE",
					headers: headers );

				return true;
			},
			$"Failed to acknowledge server action ({actionId})" );
	}
}
