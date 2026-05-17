using Dxura.RP.Shared;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public static partial class ServerApiClient
{
	public static async Task<ItemDefinitionDto?> GetItemDefinition( Guid itemId )
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return null;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<ItemDefinitionDto>(
					$"{Constants.ApiBaseUrl}/v1/server/inventory/items/{itemId}",
					"GET", headers: headers );

				return response;
			},
			$"Failed to get item definition ({itemId})" );
	}

	public static async Task<InventoryItemDto?> GivePlayerItem( long playerId, GiveItemDto dto )
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return null;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<InventoryItemDto>(
					$"{Constants.ApiBaseUrl}/v1/server/inventory/{playerId}/give",
					"POST", Http.CreateJsonContent( dto ), headers );

				return response;
			},
			$"Failed to give item to player {playerId}" );
	}

	public static async Task<bool> TakePlayerItem( long playerId, TakeItemDto dto )
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return false;
		}

		return await SafeApiCall( async headers =>
			{
				await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/server/inventory/{playerId}/take",
					"POST", Http.CreateJsonContent( dto ), headers );

				return true;
			},
			$"Failed to take item from player {playerId}" );
	}

	public static async Task<List<InventoryItemDto>?> GetPlayerInventory( long playerId )
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return null;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<List<InventoryItemDto>>(
					$"{Constants.ApiBaseUrl}/v1/server/inventory/{playerId}",
					"GET", headers: headers );

				return response;
			},
			$"Failed to get inventory for player {playerId}" );
	}

}
