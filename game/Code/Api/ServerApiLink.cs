using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Dxura.RP.Shared;
using Sandbox.Network;
using System.Threading;
namespace Dxura.RP.Game;

/// <summary>
/// Handles external API links for the game (e.g. handing server actions)
/// SERVER ONLY
/// </summary>
public partial class ServerApiLink : GameObjectSystem<ServerApiLink>, IGameEvents
{
	[ConVar( "authorize" )]
	public static string Token { get; set; } = "";

	[ConVar( "api", ConVarFlags.Saved )]
	public static string Api { get; set; } = "production";

	public static bool HasAuthorizationKey => !string.IsNullOrWhiteSpace( Token );
	public static ApiEndpoint Endpoint => ResolveEndpoint();

	private static ApiEndpoint ResolveEndpoint()
	{
		return Api.Trim().ToLowerInvariant() switch
		{
			"local" => ApiEndpoint.Local,
			"localhost" => ApiEndpoint.Local,
			"staging" => ApiEndpoint.Staging,
			"stage" => ApiEndpoint.Staging,
			"prod" => ApiEndpoint.Production,
			"production" => ApiEndpoint.Production,
			_ => ApiEndpoint.Production
		};
	}

	private Dictionary<long, List<BanDto>> GlobalBans { get; set; } = new();
	private TimeSince LastPulseTime { get; set; } = 0;

	private bool _isInitialized;
	private int _isPulsing;

	/// <summary>
	/// The tenant ID for this server instance
	/// </summary>
	[Property]
	[Sync( SyncFlags.FromHost )]
	public string TenantId { get; private set; } = string.Empty;

	[Property]
	[Sync( SyncFlags.FromHost )]
	public Guid ServerId { get; private set; }
	
	[Property]
	[Sync( SyncFlags.FromHost )]
	public Guid? RulesetId { get; set; }
	
	public ServerApiLink( Scene scene ) : base( scene )
	{
		Listen( Stage.SceneLoaded, 0, RegisterActionHandlers, "Register Action Handlers" );
	}

	public async Task Initialize()
	{
		await GameTask.MainThread();
		var initializeResponse = await ServerApiClient.InitializeServer( new InitalizeServerDto
		{
			Version = Application.Version, DefaultConfig = Json.Serialize( Config.Current.Game )
		} );
		await GameTask.MainThread();

		// If null, we're not allowed to run...
		if ( initializeResponse == null )
		{
			Log.Warning( "You are not authorized to run a server" );
			Sandbox.Game.Close();
			return;
		}

		GameNetworkManager.ServerName = initializeResponse.Name;
		GameNetworkManager.MaxPlayers = initializeResponse.MaxPlayers;
		GameNetworkManager.WhitelistRankIds = initializeResponse.WhitelistRankIds.ToArray();
		
		RulesetId = initializeResponse.RulesetId;
		Config.Current.SetGameMode( initializeResponse.GameMode );
		
		RankSystem.Instance.SetRanks( initializeResponse.Ranks );
		RankSystem.Instance.SetRankAssignments( initializeResponse.RankAssignments );

		Config.ApplyOverride( initializeResponse.OverrideConfig );
		Config.Current.MarkReady();

		// Store IDs for this server instance
		TenantId = initializeResponse.TenantId.ToString();
		ServerId = initializeResponse.Id;
		
		// Create lobby
		Networking.CreateLobby( new LobbyConfig
		{
			Name = GameNetworkManager.ServerName
		} );
		
		// Set the map (with prefab if configured)
		SetupMap( initializeResponse.MapSboxIdentifier, initializeResponse.MapPrefabJson );


		_isInitialized = true;
		LastPulseTime = 0;

		// Initial Pulse
		QueuePulse();
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost || Scene.IsEditor || !HasAuthorizationKey || !Networking.IsActive )
		{
			return;
		}

		if ( !_isInitialized )
		{
			return;
		}

		// Do we need to pulse?
		if ( LastPulseTime.Relative < Constants.ApiServerSyncInterval )
		{
			return;
		}

		QueuePulse();

		LastPulseTime = 0;
	}

	private void QueuePulse()
	{
		if ( Interlocked.CompareExchange( ref _isPulsing, 1, 0 ) != 0 )
		{
			return;
		}

		_ = Pulse();
	}

	private async Task Pulse()
	{
		try
		{
			var playerIds = GameUtils.Players
				.Where( x => x.IsValid() && x.Connection?.SteamId.Value != null )
				.Select( x => x.Connection!.SteamId.Value )
				.ToArray();

			var hostStats = HostStatsTracker.Current?.GetStats();

			var response = await ServerApiClient.Pulse( new ServerPulseDto
			{
				PlayerIds = playerIds,
				HostStats = hostStats
			} );

			if ( response == null )
			{
				return;
			}

			await GameTask.MainThread();

			GlobalBans = response.Bans
				.GroupBy( ban => ban.PlayerId )
				.ToDictionary( g => g.Key, g => g.ToList() );

			EnforceBansOnConnectedPlayers();

			await HandleServerActions( response.PendingActions );
		}
		finally
		{
			Interlocked.Exchange( ref _isPulsing, 0 );
		}
	}

	private void SetupMap( string? mapIdent, string? prefabJson )
	{
		GameObject? prefab = null;

		if ( !string.IsNullOrEmpty( prefabJson )
		     && JsonNode.Parse( prefabJson ) is JsonObject obj
		     && obj["RootObject"] is JsonObject rootObject )
		{
			prefab = new GameObject();
			prefab.Deserialize( rootObject );
		}

		MapFitter.Fit( mapIdent, prefab );
	}

	/// <summary>
	///     Kicks any connected players that have an active ban
	/// </summary>
	private void EnforceBansOnConnectedPlayers()
	{
		foreach ( var player in GameUtils.Players )
		{
			if ( !player.IsValid() || player.Connection == null )
			{
				continue;
			}

			var banMessage = CheckForBan( player.Connection );
			if ( banMessage != null )
			{
				Log.Info( $"Enforcing ban on connected player {player.DisplayName} ({player.SteamId}): {banMessage}" );
				GameNetworkManager.Instance.KickPlayer( player.Connection, banMessage, true );
			}
		}
	}

	/// <summary>
	///     Checks if a player is banned from the system and returns the ban message if they are
	/// </summary>
	public string? CheckForBan( Connection connection )
	{
		if ( !Networking.IsHost || Config.Current == null || !HasAuthorizationKey )
		{
			return null;
		}

		// Check for bans
		var bans = Current.GlobalBans.Where( x => x.Key == connection.SteamId.Value || x.Key == connection.OwnerSteamId.Value )
			.SelectMany(  x => x.Value )
			.ToList();
		
		foreach ( var ban in bans )
		{
			if ( ban.IsGlobal )
			{
				return $"You are globally banned ({ban.Reason})";
			}

			if ( !ban.Duration.HasValue )
			{
				return $"You are permanently banned ({ban.Reason})";
			}

			var timeRemaining = (ban.Created + ban.Duration.Value) - DateTimeOffset.UtcNow;
			if ( timeRemaining.TotalSeconds <= 0 )
			{
				continue;
			}

			// Convert time remaining to hours (rounded up)
			var hoursRemaining = (int)Math.Ceiling( timeRemaining.TotalHours );
			var hourText = hoursRemaining == 1 ? "hour" : "hours";
			return $"You are banned ({ban.Reason}), expires in {hoursRemaining} {hourText}";
		}

		return null;
	}
}
