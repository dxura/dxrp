using Dxura.RP.Shared;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public static partial class ServerApiClient
{
	public static async Task<GameModeDto?> FetchDefaultGameMode()
	{
		return await ApiClientBase.SafeApiCall(
			() => ApiClientBase.RequestJsonAsync<GameModeDto>( $"{Constants.ApiBaseUrl}/v1/public/gamemode/default" ),
			"Failed to fetch default game mode" );
	}

	public static async Task<InitalizeServerResponseDto?> InitializeServer( InitalizeServerDto initalize )
	{
		return await SafeApiCall<InitalizeServerResponseDto?>( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<InitalizeServerResponseDto>(
					$"{Constants.ApiBaseUrl}/v1/server/initialize",
					"POST", Http.CreateJsonContent( initalize ), headers );

				return response;
			},
			"Failed to initialize server with API" );
	}

	public static async Task<InitalizePlayerResponseDto?> InitializePlayer( InitalizePlayerDto initalize )
	{
		return await SafeApiCall<InitalizePlayerResponseDto?>( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<InitalizePlayerResponseDto>(
					$"{Constants.ApiBaseUrl}/v1/server/player/initialize",
					"POST", Http.CreateJsonContent( initalize ), headers );

				return response;
			},
			$"Failed to initialize player (Name: {initalize.Name})" );
	}

	public static async Task<ServerPulseResponseDto?> Pulse( ServerPulseDto pulse )
	{
		return await SafeApiCall<ServerPulseResponseDto?>( async headers =>
			{
				var httpResponse = await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/server/pulse", "POST", Http.CreateJsonContent( pulse ), headers );

				httpResponse.EnsureSuccessStatusCode();
				var body = await httpResponse.Content.ReadAsStringAsync();
				
				// HACK: Fixes unrecognized type discriminator id 'set_level'. etc when new actions are added without restart.
				return ParsePulseResponse( body );
			},
			"Failed to pulse server" );
	}

	private static readonly JsonSerializerOptions PulseOptions = new() { PropertyNameCaseInsensitive = true };

	private static ServerPulseResponseDto ParsePulseResponse( string body )
	{
		var node = JsonNode.Parse( body )!.AsObject();

		var safeActions = node["pendingActions"]!.AsArray()
			.Select( e => { try { return e.Deserialize<ServerActionDto>( PulseOptions ); } catch ( JsonException ) { return null; } } )
			.OfType<ServerActionDto>()
			.ToList();

		node["pendingActions"] = JsonSerializer.SerializeToNode( safeActions, PulseOptions );

		return node.Deserialize<ServerPulseResponseDto>( PulseOptions )!;
	}

	public static async Task<bool> SanctionPlayer( long playerId, CreateSanctionDto sanction )
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return false;
		}

		var succeeded = await SafeApiCall( async headers =>
			{
				await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/server/moderation/sanction/{playerId}",
					"POST",
					Http.CreateJsonContent( sanction ),
					headers );

				return true;
			},
			$"Failed to sanction player {playerId} for {sanction.Reason}" );

		if ( succeeded )
		{
			PlayerSanctionHistorySystem.Current?.InvalidateCachedSanctions( playerId );
		}

		return succeeded;
	}

	public static bool Audit( string action, string description, long? cause = null )
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			Log.Info( $"[{action}] {description}" );
			return false;
		}
		
		return TryQueueApiCall( async headers =>
			{
				Log.Info( $"[{action}] {description}" );

				var url = $"{Constants.ApiBaseUrl}/v1/server/audit?action={action.UrlEncode()}&description={description.UrlEncode()}";
				if ( cause.HasValue )
				{
					url += $"&cause={cause.Value}";
				}

				await ApiClientBase.RequestAsync( url, "POST", headers: headers );
			},
			$"Failed to audit (Action {action} Description {description})" );
	}

	public static async Task<bool> SetRpName( long steamId, string? rpName )
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return true;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<bool>(
					$"{Constants.ApiBaseUrl}/v1/server/players/{steamId}/rpname",
					"POST",
					Http.CreateJsonContent( rpName ),
					headers );

				return response;
			},
			$"Failed to set RP name for player ({steamId})" );
	}

	public static async Task<bool> ModifyPlayerBalance( long steamId, int amount, string reason )
	{
		if ( !ServerApiLink.HasAuthorizationKey || !Config.Current.Game.MoneyEnabled )
		{
			return true;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<bool>(
					$"{Constants.ApiBaseUrl}/v1/server/players/{steamId}/balance?amount={amount}&reason={reason.UrlEncode()}",
					"POST",
					headers: headers );

				return response;
			},
			$"Failed to modify player ({steamId}) balance (Amount: {amount})" );
	}

	public static async Task<List<PlayerSanctionHistoryDto>> GetPlayerSanctions( long playerId )
	{
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			return [];
		}

		var result = await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestJsonAsync<List<PlayerSanctionHistoryDto>>(
					$"{Constants.ApiBaseUrl}/v1/server/players/{playerId}/sanctions",
					"GET",
					headers: headers );

				return response;
			},
			$"Failed to get sanctions for player {playerId}" );

		return result ?? [];
	}
}
