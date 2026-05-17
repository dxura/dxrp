using Dxura.RP.Game.System.Events;
using Dxura.RP.Game.UI;
using Dxura.RP.Shared;
using Sandbox.Diagnostics;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public class AdminSystem : SingletonComponent<AdminSystem>
{
	[Property] [Group( "Spawns" )] public required GameObject NpcPrefab { get; set; }
	internal readonly Dictionary<long, (Vector3 Position, Rotation Rotation)> PlayerReturnPositions = new();

	[Rpc.Host]
	public void KickPlayerHost( long steamId, string reason )
	{
		var callerId = Rpc.CallerId;
		var callerSteamId = Rpc.Caller.SteamId;

		if ( !RankSystem.HasPermission( callerSteamId, Permission.PlayerKick ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		var kickPlayer = GameUtils.GetPlayerById( steamId );

		if ( !player.IsValid() || !kickPlayer.IsValid() )
		{
			return;
		}

		if ( !RankSystem.CanTarget( callerSteamId, kickPlayer.SteamId ) )
		{
			return;
		}

		GameNetworkManager.Instance.KickPlayer( kickPlayer.Connection, reason );

		_ = ServerApiClient.Audit( "Kick", $"{player.SteamName} ({player.SteamId}) has kicked {kickPlayer.SteamName} ({steamId}) for {reason}", player.SteamId );
		_ = ServerApiClient.SanctionPlayer( steamId, new CreateSanctionDto
		{
			Reason = reason, Notes = $"Kicked by {player.SteamName} ({player.SteamId}) in-game.", Type = SanctionType.Kick
		} );
	}

	[Rpc.Host( NetFlags.Unreliable )]
	public void MovePlayerHost( long steamId, Vector3 targetPosition )
	{
		var callerSteamId = Rpc.Caller.SteamId;

		if ( !RankSystem.HasPermission( callerSteamId, Permission.PlayerGrab ) )
		{
			return;
		}

		var targetPlayer = GameUtils.GetPlayerById( steamId );

		if ( !targetPlayer.IsValid() )
		{
			return;
		}

		if ( !RankSystem.CanTarget( callerSteamId, targetPlayer.SteamId ) )
		{
			return;
		}

		targetPlayer.TeleportHost( new Transform( targetPosition, targetPlayer.WorldRotation ) );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	internal void BroadcastTeleportEffect( Player player, Vector3 fromPosition, Vector3 toPosition )
	{
		if ( !player.IsValid() || player.HasStatus( Constants.CloakStatus ) )
		{
			return;
		}

		Sound.Play( "teleport", fromPosition );
		Sound.Play( "teleport", toPosition );
	}

	[Rpc.Host]
	public void SpawnNpcHost()
	{
		var callerSteamId = Rpc.Caller.SteamId;

		if ( !RankSystem.HasPermission( callerSteamId, Permission.DebugFull ) )
		{
			return;
		}

		var callerPlayer = GameUtils.GetPlayerById( callerSteamId );
		if ( !callerPlayer.IsValid() )
		{
			return;
		}

		var npcClone = NpcPrefab.Clone( callerPlayer.WorldPosition + Vector3.Forward * 50, callerPlayer.WorldRotation );

		npcClone.NetworkSpawn();
	}

	[Rpc.Host]
	public void BankAllHost()
	{
		if ( !RankSystem.HasPermission( Rpc.Caller.SteamId, Permission.ManageEconomy ) )
		{
			return;
		}

		_ = BankAll();
	}

	[Rpc.Host]
	public void SetWireTick( float? tick )
	{
		if ( !RankSystem.HasPermission( Rpc.Caller.SteamId, Permission.DebugFull ) )
		{
			return;
		}

		Wire.Wire.Current.WireTickOverride = tick;
	}

	[Rpc.Host]
	public void ForceScreenshotHost( long steamId )
	{
		var canExecute = Rpc.Caller.IsHost || RankSystem.HasPermission( Rpc.Caller.SteamId, Permission.ForceScreenshot );
		if ( !canExecute || !ServerApiLink.HasAuthorizationKey )
		{
			return;
		}

		var player = GameUtils.GetPlayerById( steamId );
		if ( !player.IsValid() )
		{
			return;
		}

		using ( Rpc.FilterInclude( c => c == player.Connection ) )
		{
			BroadcastForceScreenshot();
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastForceScreenshot()
	{
		_ = GameTask.RunInThreadAsync( async () =>
		{
			var texture = Texture.CreateRenderTarget().WithSize(  1920,1080 ).Create();

			try
			{
				await GameTask.MainThread();
				Scene.Camera.RenderToTexture( texture );
				await GameTask.WorkerThread();

				var bitmap = texture.GetBitmap( 0 );
				var payload = bitmap.ToPng();

				// Send screenshot to server API
				await PlayerApiClient.ShareScreenshot( payload, true );
			}
			finally
			{
				texture.Dispose();
			}
		} );
	}

	private async Task BankAll()
	{
		Assert.True( Networking.IsHost );

		var total = 0;
		foreach ( var player in GameUtils.Players.ToList() )
		{
			var amount = player.WalletBalance;
			var didTakeout = await player.ChargeHost( amount, "BankAll" );

			if ( !didTakeout )
			{
				continue;
			}

			var didBank = await player.PayHost( amount, "BankAll", true );

			if ( !didBank )
			{
				await player.PayHost( amount, "BankAll" );
				continue;
			}

			total += (int)amount;
		}

		Chat.Current?.BroadcastSystemText( $"All wallets have been banked (totalling {total:C0})" );
	}

	[Rpc.Host]
	public void RestartHost( string reason, bool snapshot, float delaySeconds = 120 )
	{
		var caller = Rpc.Caller;
		var rpcCaller = Rpc.Caller;

		if ( !RankSystem.HasPermission( Rpc.Caller.SteamId, Permission.ServerRestart ) )
		{
			return;
		}

		Chat.Current?.BroadcastSystemText( $"{caller.DisplayName} has initiated a server restart" );

		_ = Restart( reason, snapshot, delaySeconds );
	}

	public async Task Restart( string reason, bool snapshot, float delaySeconds = 5 )
	{
		Assert.True( Networking.IsHost );

		Chat.Current?.BroadcastSystemText( $"Server is restarting ({reason}) in {delaySeconds} seconds." );

		if ( snapshot )
		{
			await SnapshotSystem.Current.SaveSnapshot();
		}

		// Wait for the specified delay, announcing every 10 seconds
		var remainingSeconds = delaySeconds;
		while ( remainingSeconds > 0 )
		{
			await GameTask.DelayRealtimeSeconds( Math.Min( 10, remainingSeconds ) );
			remainingSeconds -= Math.Min( 10, remainingSeconds );

			if ( remainingSeconds > 0 )
			{
				Chat.Current?.BroadcastSystemText( $"Server is restarting in {remainingSeconds} seconds..." );
			}
		}

		Chat.Current?.BroadcastSystemText( "Server is restarting now..." );

		using ( Rpc.FilterExclude( c => c.IsHost ) )
		{
			BroadcastEjectToWaitingRoom();
		}

		await GameTask.DelayRealtimeSeconds( 5 );

		Sandbox.Game.Close();
	}

	[Rpc.Host]
	public void NukeHost()
	{
		var rpcCaller = Rpc.Caller;

		if ( !RankSystem.HasPermission( Rpc.Caller.SteamId, Permission.DebugFull ) )
		{
			return;
		}

		Log.Warning( "Nuke from " + rpcCaller.DisplayName + " (" + rpcCaller.SteamId + ")" );

		Sandbox.Game.Close();
	}

	[Rpc.Host]
	public void FreezeConstructs()
	{
		var rpcCaller = Rpc.Caller;

		if ( !RankSystem.HasPermission( Rpc.Caller.SteamId, Permission.ServerRestart ) )
		{
			return;
		}

		Log.Warning( "Construct freeze from " + rpcCaller.DisplayName + " (" + rpcCaller.SteamId + ")" );

		var freezeCount = 0;
		var constructs = Sandbox.Game.ActiveScene.Components.GetAll<IConstruct>( FindMode.EverythingInSelfAndDescendants );
		foreach ( var construct in constructs )
		{
			freezeCount += construct.IsFrozen ? 0 : 1;
			construct.Freeze( construct.GameObject.WorldPosition, construct.GameObject.WorldRotation );
		}

		rpcCaller.SendLog( LogLevel.Info, $"Froze {freezeCount} constructs." );
	}

	[Rpc.Host]
	public void ToggleEventHost( string eventIdentifier )
	{
		var caller = Rpc.Caller;
		var callerSteamId = Rpc.Caller.SteamId;

		if ( !RankSystem.HasPermission( callerSteamId, Permission.Noclip ) )
		{
			return;
		}

		var eventSystem = EventSystem.Instance;
		if ( !eventSystem.IsValid() )
		{
			Log.Warning( "EventSystem not found" );
			return;
		}

		if ( string.IsNullOrEmpty( eventIdentifier ) )
		{
			Log.Warning( "Invalid event identifier" );
			return;
		}

		eventSystem.Toggle( eventIdentifier );
	}

	[Rpc.Host]
	public void AddStatusHost( string playerName, string statusId, float? duration )
	{
		var callerSteamId = Rpc.Caller.SteamId;

		if ( !RankSystem.HasPermission( callerSteamId, Permission.Noclip ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		var matchingPlayers = GameUtils.GetPlayersByName( playerName );

		if ( matchingPlayers.Count == 0 )
		{
			if ( caller.IsValid() )
			{
				caller.Error( $"Player '{playerName}' not found" );
			}
			return;
		}

		if ( matchingPlayers.Count > 1 )
		{
			if ( caller.IsValid() )
			{
				var playerNames = string.Join( ", ", matchingPlayers.Select( p => p.DisplayName ) );
				caller.SendMessage( $"Multiple players found matching '{playerName}': {playerNames}" );
			}
			return;
		}

		var target = matchingPlayers[0];

		Status.Current.AddStatus( target, statusId, duration );

		// Notify the caller
		if ( caller.IsValid() )
		{
			var durationText = duration.HasValue ? $" for {duration.Value}s" : "";
			caller.SendMessage( $"Added status '{statusId}' to {target.DisplayName}{durationText}" );
		}

		// Log the action
		var logDurationText = duration.HasValue ? $" for {duration.Value}s" : "";
		Log.Info( $"[ADMIN] {caller?.DisplayName} ({callerSteamId}) added status '{statusId}' to {target.DisplayName} ({target.SteamId}){logDurationText}" );
		_ = ServerApiClient.Audit( "Status", $"{caller?.SteamName} ({callerSteamId}) added status '{statusId}' to {target.SteamName} ({target.SteamId}){logDurationText}", caller?.SteamId );
	}

	[Rpc.Host]
	public void RemoveStatusHost( string playerName, string statusId )
	{
		var callerSteamId = Rpc.Caller.SteamId;

		if ( !RankSystem.HasPermission( callerSteamId, Permission.Noclip ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		var matchingPlayers = GameUtils.GetPlayersByName( playerName );

		if ( matchingPlayers.Count == 0 )
		{
			if ( caller.IsValid() )
			{
				caller.Error( $"Player '{playerName}' not found" );
			}
			return;
		}

		if ( matchingPlayers.Count > 1 )
		{
			if ( caller.IsValid() )
			{
				var playerNames = string.Join( ", ", matchingPlayers.Select( p => p.DisplayName ) );
				caller.SendMessage( $"Multiple players found matching '{playerName}': {playerNames}" );
			}
			return;
		}

		var target = matchingPlayers[0];

		Status.Current.RemoveStatus( target, statusId );

		// Notify the caller
		if ( caller.IsValid() )
		{
			caller.SendMessage( $"Removed status '{statusId}' from {target.DisplayName}" );
		}

		// Log the action
		Log.Info( $"[ADMIN] {caller?.DisplayName} ({callerSteamId}) removed status '{statusId}' from {target.DisplayName} ({target.SteamId})" );
		_ = ServerApiClient.Audit( "Status", $"{caller?.SteamName} ({callerSteamId}) removed status '{statusId}' from {target.SteamName} ({target.SteamId})", caller?.SteamId );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private static void BroadcastEjectToWaitingRoom()
	{
		if ( !GameManager.Instance.IsValid() )
		{
			return;
		}

		GameManager.Instance.EjectToWaitingRoom();
	}

	[Rpc.Host]
	public void RequestConnectionStatsHost()
	{
		var caller = Rpc.Caller;
		var callerSteamId = Rpc.Caller.SteamId;

		if ( !RankSystem.HasPermission( callerSteamId, Permission.DebugFull ) )
		{
			return;
		}

		foreach ( var connection in Connection.All )
		{
			caller.SendLog( LogLevel.Info, $"""
			                                			Connection ({connection.DisplayName}):
			                                			  ID: {connection.Id}
			                                			  SteamID: {connection.SteamId}
			                                			  Address: {connection.Address}
			                                			  Ping: {connection.Ping}
			                                			  Latency: {connection.Latency}
			                                			  Quality: {connection.Stats.ConnectionQuality}
			                                			  InBytesPerSecond: {connection.Stats.InBytesPerSecond}
			                                			  InPacketsPerSecond: {connection.Stats.InPacketsPerSecond}
			                                			  OutBytesPerSecond: {connection.Stats.OutBytesPerSecond}
			                                			  OutPacketsPerSecond: {connection.Stats.OutPacketsPerSecond}
			                                			  SendRateBytesPerSecond: {connection.Stats.SendRateBytesPerSecond}
			                                			  MessagesReceived: {connection.MessagesRecieved}
			                                			  MessagesSent: {connection.MessagesSent}
			                                			  Connection Time: {connection.ConnectionTime}
			                                			-----------------------------
			                                """ );
		}
	}
}
