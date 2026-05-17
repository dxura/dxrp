using Dxura.RP.Shared;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public static partial class ServerApiClient
{
	public static async Task<FactionDto?> CreateFaction( CreateFactionDto dto )
	{
		if ( !ServerApiLink.HasAuthorizationKey || !Config.Current.Game.FactionsEnabled )
		{
			return null;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<FactionDto>(
					$"{Constants.ApiBaseUrl}/v1/server/faction",
					"POST", Http.CreateJsonContent( dto ), headers );

				return response;
			},
			$"Failed to create faction (Name: {dto.Name})" );
	}

	public static async Task<List<FactionDto>?> GetAllFactions()
	{
		if ( !ServerApiLink.HasAuthorizationKey || !Config.Current.Game.FactionsEnabled )
		{
			return null;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<List<FactionDto>>(
					$"{Constants.ApiBaseUrl}/v1/server/faction",
					"GET", headers: headers );

				return response;
			},
			"Failed to get all factions" );
	}

	public static async Task<FactionDto?> GetFaction( Guid id )
	{
		if ( !ServerApiLink.HasAuthorizationKey || !Config.Current.Game.FactionsEnabled )
		{
			return null;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<FactionDto>(
					$"{Constants.ApiBaseUrl}/v1/server/faction/{id}",
					"GET", headers: headers );

				return response;
			},
			$"Failed to get faction ({id})" );
	}

	public static async Task<FactionDto?> UpdateFaction( Guid id, UpdateFactionDto dto )
	{
		if ( !ServerApiLink.HasAuthorizationKey || !Config.Current.Game.FactionsEnabled )
		{
			return null;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<FactionDto>(
					$"{Constants.ApiBaseUrl}/v1/server/faction/{id}",
					"PUT", Http.CreateJsonContent( dto ), headers );

				return response;
			},
			$"Failed to update faction ({id})" );
	}

	public static async Task DeleteFaction( Guid id )
	{
		if ( !ServerApiLink.HasAuthorizationKey || !Config.Current.Game.FactionsEnabled )
		{
			return;
		}

		await SafeApiCall( async headers =>
			{
				await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/server/faction/{id}",
					"DELETE", headers: headers );

				return true;
			},
			$"Failed to delete faction ({id})" );
	}

	public static async Task RemoveFactionMember( Guid factionId, long playerId )
	{
		if ( !ServerApiLink.HasAuthorizationKey || !Config.Current.Game.FactionsEnabled )
		{
			return;
		}

		await SafeApiCall( async headers =>
			{
				await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/server/faction/{factionId}/members/{playerId}",
					"DELETE", headers: headers );

				return true;
			},
			$"Failed to remove member ({playerId}) from faction ({factionId})" );
	}

	public static async Task AddFactionMember( Guid factionId, AddFactionMemberDto dto )
	{
		if ( !ServerApiLink.HasAuthorizationKey || !Config.Current.Game.FactionsEnabled )
		{
			return;
		}

		await SafeApiCall( async headers =>
			{
				await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/server/faction/{factionId}/members",
					"POST", Http.CreateJsonContent( dto ), headers );

				return true;
			},
			$"Failed to add member to faction ({factionId})" );
	}

	public static async Task<FactionRoleDto?> CreateFactionRole( Guid factionId, CreateFactionRoleDto dto )
	{
		if ( !ServerApiLink.HasAuthorizationKey || !Config.Current.Game.FactionsEnabled )
		{
			return null;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<FactionRoleDto>(
					$"{Constants.ApiBaseUrl}/v1/server/faction/{factionId}/roles",
					"POST", Http.CreateJsonContent( dto ), headers );

				return response;
			},
			$"Failed to create faction role (Faction: {factionId})" );
	}

	public static async Task<FactionRoleDto?> UpdateFactionRole( Guid factionId, Guid roleId, UpdateFactionRoleDto dto )
	{
		if ( !ServerApiLink.HasAuthorizationKey || !Config.Current.Game.FactionsEnabled )
		{
			return null;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<FactionRoleDto>(
					$"{Constants.ApiBaseUrl}/v1/server/faction/{factionId}/roles/{roleId}",
					"PUT", Http.CreateJsonContent( dto ), headers );

				return response;
			},
			$"Failed to update faction role (Faction: {factionId}, Role: {roleId})" );
	}

	public static async Task DeleteFactionRole( Guid factionId, Guid roleId )
	{
		if ( !ServerApiLink.HasAuthorizationKey || !Config.Current.Game.FactionsEnabled )
		{
			return;
		}

		await SafeApiCall( async headers =>
			{
				await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/server/faction/{factionId}/roles/{roleId}",
					"DELETE", headers: headers );

				return true;
			},
			$"Failed to delete faction role (Faction: {factionId}, Role: {roleId})" );
	}
}
