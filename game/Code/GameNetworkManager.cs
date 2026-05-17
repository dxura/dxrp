using System.Threading.Tasks;
using Dxura.RP.Game.UI;
using Dxura.RP.Shared;
using Sandbox.Diagnostics;
using Sandbox.Network;

namespace Dxura.RP.Game;

[Title( "Game Network Manager" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]
public sealed class GameNetworkManager : SingletonComponent<GameNetworkManager>, Component.INetworkListener, IGameEvents
{
	/// <summary>
	///     The prefab to spawn for the player to control.
	/// </summary>
	[Property]
	public required GameObject PlayerPrefab { get; set; }

	//
	// Players
	//

	[Property]
	[ReadOnly]
	[Sync( SyncFlags.FromHost )]
	public NetDictionary<long, Player> Players { get; set; } = new();

	// Local caches for fast access (updated every second)
	public Dictionary<Guid, Player> PlayersByConnectionIdCache { get; } = new();
	private TimeSince LastPlayersCacheUpdate { get; set; } = 0;
	private TimeSince LastOrphanedConnectionCheck { get; set; } = 0;
	
	private TimeSince LastHeadlessServerOptimization { get; set; } = 0;
	private TimeSince LastServerHeartbeat { get; set; } = 0;

	public static string ServerName = "Unknown Server";
	public static int MaxPlayers = 128;
	public static Guid[] WhitelistRankIds = [];

	/// <summary>
	///     Called when a network connection becomes active
	/// </summary>
	public void OnActive( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' is joining the game" );

		// Announce player join in chat
		Chat.Current?.BroadcastSystemText( string.Format( Language.GetPhrase( "system.player.joined" ), channel.DisplayName.Replace( "(2)", string.Empty ).Trim() ) );

		_ = CreateOrReusePlayer( channel );
	}

	public void OnDisconnected( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' has left the game" );

		// Announce player leave in chat
		Chat.Current?.BroadcastSystemText( string.Format( Language.GetPhrase( "system.player.left" ), channel.DisplayName ) );
	}

	public bool AcceptConnection( Connection channel, ref string reason )
	{
		if ( !Application.IsEditor && !Config.Current.Game.AllowFamilySharePlayers && channel.OwnerSteamId != channel.SteamId )
		{
			reason = "Family shared accounts are not permitted on this server.";
			return false;
		}
		
		if ( !ServerApiLink.HasAuthorizationKey ) return true;

		// Check if player is banned globally
		var banMessage = ServerApiLink.Current.CheckForBan( channel );
		if ( banMessage != null )
		{
			reason = banMessage;
			return false;
		}

		// Check if server is full
		var isAlreadyConnected = GameUtils.GetPlayerById( channel.SteamId ).IsValid();
		var isStaff = RankSystem.HasPermission( channel.SteamId, Permission.BypassMaxPlayers );
		if ( Connection.All.Count > MaxPlayers && !isAlreadyConnected && !isStaff && !Application.IsEditor )
		{
			reason = Language.GetPhrase( "system.server.full" );
			return false;
		}

		if ( WhitelistRankIds.Length > 0 && !RankSystem.IsRankWhitelisted( channel.SteamId, WhitelistRankIds ) )
		{
			reason = "You are not whitelisted for this server.";
			return false;
		}
		
		return true;
	}

	protected override void OnStart()
	{
		// Allow in editor mode for testing or ignore for clients
		if ( Scene.IsEditor || Networking.IsConnecting || Networking.IsClient )
		{
			return;
		}

		LoadingScreen.Title = Language.GetPhrase( "system.server.initializing" );
		MaxPlayers = Networking.MaxPlayers;

		// Create lobby from the authorized server API when a key is provided, otherwise use the public API defaults.
		if ( ServerApiLink.HasAuthorizationKey )
		{
			_ = ServerApiLink.Current.Initialize();
		}
		else
		{
			_ = InitializeWithPublicApiDefaults();
		}
	}

	private async Task InitializeWithPublicApiDefaults()
	{
		var gameMode = await ServerApiClient.FetchDefaultGameMode();
		await GameTask.MainThread();
		Config.Current.SetGameMode( gameMode ?? GameModeDto.Default );
		Config.Current.MarkReady();
		Networking.CreateLobby( new LobbyConfig()
		{
			Name = ServerName,
			MaxPlayers = MaxPlayers,
		} );
	}

	public void OnSecondlyUpdate()
	{
		UpdatePlayerCaches();
		HandleOrphanedConnections();
		HandleHeartbeat();
		HandleHeadlessServerOptimizations();
	}

	private void HandleOrphanedConnections()
	{
		if ( !Networking.IsHost || !Networking.IsActive )
		{
			return;
		}

		if ( !(LastOrphanedConnectionCheck.Relative > 30) )
		{
			return;
		}

		LastOrphanedConnectionCheck = 0;

		foreach ( var connection in Connection.All )
		{
			if ( connection.IsHost || !connection.IsActive )
			{
				continue;
			}

			var connectionAge = DateTimeOffset.UtcNow - connection.ConnectionTime;
			if ( connection.IsConnecting || connectionAge < TimeSpan.FromMinutes( 5 ) )
			{
				continue;
			}

			var player = GameUtils.GetPlayerByConnectionId( connection.Id );
			if ( player.IsValid() )
			{
				continue;
			}

			Log.Warning( $"Kicking orphaned connection '{connection.DisplayName}' ({connection.SteamId}) after {connectionAge.TotalSeconds:0.0}s without a player" );
			connection.Kick( "Failed to initialize player" );
		}
	}

	private void HandleHeadlessServerOptimizations()
	{
		if ( !Networking.IsHost || !Networking.IsActive || !GameManager.IsHeadless )
		{
			return;
		}
		
		if ( !(LastHeadlessServerOptimization.Relative > 300) )
		{
			return;
		}
		
		LastHeadlessServerOptimization = 0;
		
		var skinnedModelRenderers = Scene.GetAllComponents<SkinnedModelRenderer>();
		foreach ( var skinnedModelRenderer in skinnedModelRenderers )
		{
			Log.Warning( $"Disabling SkinnedModelRenderer '{skinnedModelRenderer.GameObject.Name}' for headless optimization" );
			skinnedModelRenderer.Enabled = false;
		}
		
		var playerControllers = Scene.GetAllComponents<PlayerController>();
		foreach ( var playerController in playerControllers )
		{
			Log.Warning( $"Disabling PlayerController '{playerController.GameObject.Name}' for headless optimization" );
			playerController.Enabled = false;
		}
	}

	private void HandleHeartbeat()
	{
		if ( !Networking.IsActive )
		{
			return;
		}

		// Host side: send heartbeat
		if ( Networking.IsHost )
		{
			if ( LastServerHeartbeat < 5 )
			{
				return;
			}

			BroadcastServerHeartbeat();
			LastServerHeartbeat = 0;

			return;
		}

		// Client side: check for heartbeat timeout
		if ( LastServerHeartbeat < 45 || !GameManager.Instance.IsValid() || Networking.IsConnecting )
		{
			return;
		}
		
		Log.Info( "No heartbeat received from server, disconnecting..." );
		GameManager.Instance.EjectToWaitingRoom();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable | NetFlags.SendImmediate )]
	private void BroadcastServerHeartbeat()
	{
		LastServerHeartbeat = 0;
	}

	private void UpdatePlayerCaches()
	{
		if ( !(LastPlayersCacheUpdate.Relative > 1) )
		{
			return;
		}

		// Update the player caches
		PlayersByConnectionIdCache.Clear();

		var toRemove = new List<long>();

		foreach ( var (id, player) in Players )
		{
			if ( !player.IsValid() )
			{
				toRemove.Add( id );
				continue;
			}

			PlayersByConnectionIdCache[player.ConnectionId] = player;
		}

		foreach ( var id in toRemove )
		{
			Players.Remove( id );
		}

		LastPlayersCacheUpdate = 0;
	}

	private async Task CreateOrReusePlayer( Connection channel )
	{
		Log.Info( $"Creating or reusing player for '{channel.DisplayName}' ({channel.SteamId})" );
		Assert.True( PlayerPrefab.IsValid(), "Could not spawn player as no PlayerPrefab assigned." );

		// Fetch initialization data (including active sanctions) BEFORE spawning so a banned
		// player is never admitted into the world — even briefly.
		InitalizePlayerResponseDto? initalizePlayerResponse = null;
		if ( ServerApiLink.HasAuthorizationKey )
		{
			initalizePlayerResponse = await ServerApiClient.InitializePlayer( new InitalizePlayerDto
			{
				Id = channel.SteamId, Name = channel.DisplayName
			} );

			if ( initalizePlayerResponse == null )
			{
				Log.Warning( $"InitializePlayer returned null for {channel.DisplayName} ({channel.SteamId}), rejecting connection." );
				channel.Kick( "Failed to authenticate with server." );
				return;
			}

			var banSanction = initalizePlayerResponse.ActiveSanctions.FirstOrDefault( x => x.Type == SanctionType.Ban );
			if ( banSanction != null )
			{
				Log.Info( $"Rejecting banned player {channel.DisplayName} ({channel.SteamId}) before spawn: {banSanction.Reason}" );
				channel.Kick( banSanction.Reason );
				return;
			}
		}

		var existingPlayer = GameUtils.GetPlayerById( channel.SteamId );
		Player? player = null;

		// While in editor, we don't reuse players that are still connected
		if ( Application.IsEditor && existingPlayer.IsValid() && existingPlayer.IsConnected )
		{
			existingPlayer = null;
		}

		if ( existingPlayer.IsValid() )
		{
			Log.Info( $"Found existing disconnected player for {channel.DisplayName}, attempting reuse..." );

			try
			{
				player = existingPlayer;
				player.DisconnectedSince = null;

				// Assign the player to the channel
				existingPlayer.GameObject.Network.AssignOwnership( channel );

				// And the equipment too
				foreach ( var equipment in existingPlayer.Equipment )
				{
					if ( equipment.IsValid() )
					{
						equipment.Network.AssignOwnership( channel );
					}
				}

				Log.Info( $"Successfully reused existing player for {channel.DisplayName}" );
			}
			catch ( Exception ex )
			{
				Log.Warning( $"Failed to reuse existing player for {channel.DisplayName}: {ex.Message}" );
				CleanupSystem.Current.CleanupPlayer( existingPlayer.SteamId );
				player = null;
			}
		}

		// If we didn't find an existing player, create a new one
		var isNewPlayer = player == null;
		if ( isNewPlayer )
		{
			Log.Info( $"Creating new player for {channel.DisplayName}" );

			// Determine initial spawn point
			var spawnPosition = WorldPosition;
			if ( RespawnerSystem.Instance.IsValid() )
			{
				spawnPosition = RespawnerSystem.Instance.GetSpawnPoint().Position;
			}

			var playerGameObject = PlayerPrefab.Clone( new Transform( spawnPosition, Rotation.Identity ), name: $"Player ({channel.DisplayName})" );
			player = playerGameObject.GetComponent<Player>();
			player.SteamId = channel.SteamId;
			player.SteamName = channel.DisplayName;
			player.Job = GameModeJobs.Default;

			playerGameObject.NetworkSpawn( channel );
		}

		if ( !player.IsValid() )
		{
			channel.Kick( "Failed to create player (Report this)" );
			throw new Exception( $"Failed to create player for {channel.DisplayName}" );
		}

		Players[player.SteamId] = player;
		PlayersByConnectionIdCache[channel.Id] = player;

		// Permit teleport for the player to avoid anticheat triggering on spawn
		Sentinel.Sentinel.Current?.PermitPlayerTeleportHost( player.SteamId, 15f );

		if ( initalizePlayerResponse != null )
		{
			player.InitalizeHost( initalizePlayerResponse.Balance, initalizePlayerResponse.Playtime, initalizePlayerResponse.Level, initalizePlayerResponse.RpName );

			if ( !initalizePlayerResponse.PrivacyConsent )
			{
				using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
				{
					PromptPlayerConsent();
				}
			}

			if ( Config.Current.Game.StreakMessageEnabled )
			{
				var streak = initalizePlayerResponse.Streak;
				if ( streak > 0 )
				{
					player.SendMessage( string.Format( Language.GetPhrase( "system.login_streak" ), streak ) );
				}
			}
		}
		else
		{
			player.InitalizeHost( 100000, 360000, 1000 );
		}

		// Restore recovery data for new players only
		if ( isNewPlayer )
		{
			var recoveryData = SnapshotSystem.Current?.TakePlayerData( channel.SteamId );
			if ( recoveryData != null )
			{
				((ISnapshot)player).Load( recoveryData );
			}
		}

		if ( initalizePlayerResponse != null )
		{
			foreach ( var sanction in initalizePlayerResponse.ActiveSanctions )
			{
				ServerApiLink.Current.ExecuteAction( sanction );

				// Sanctions can kick/ban immediately, which cleans up the player during initialization.
				if ( !player.IsValid() || !channel.IsActive )
				{
					return;
				}
			}
		}

		if ( !player.IsValid() || !channel.IsActive )
		{
			return;
		}

		IGameEvents.Post( x => x.OnPlayerJoined( player ) );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void PromptPlayerConsent()
	{
		GameManager.ShowUi<ConsentPanel>();
	}

	/// <summary>
	///     Kicks a player with the given reason
	/// </summary>
	public void KickPlayer( Connection? connection, string reason, bool isBan = false )
	{
		if ( connection is not { IsActive: true } )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( connection.Id );
		if ( !player.IsValid() )
		{
			return;
		}

		Chat.Current?.BroadcastSystemText( $"{player.DisplayName} has been {(isBan ? "banned" : "kicked")}: {reason}" );

		if ( connection.IsHost )
		{
			Sandbox.Game.Close();
			return;
		}

		connection.Kick( reason );
		CleanupSystem.Current.CleanupPlayer( player.SteamId );
	}

}
