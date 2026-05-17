using Dxura.RP.Shared;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public class PocketSystem : SingletonComponent<PocketSystem>, IGameEvents
{
	[Property]
	private Dictionary<long, List<GameObject>> Pockets { get; set; } = new();

	private static readonly object PickupLock = new();

	/// <summary>
	///     Local pocket count for the current client (updated via RPC).
	/// </summary>
	public static int LocalPocketCount { get; private set; }


	protected override void OnStart()
	{
		if ( !Config.Current.Game.PocketEnabled )
		{
			Destroy();
			return;
		}
	}

	[Rpc.Host]
	public void PickupHost()
	{
		var caller = Rpc.Caller;
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:pocket", Config.Current.Game.PocketCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			return;
		}

		var trace = Scene.Trace.Ray( player.AimRay, Config.Current.Game.ReachDistance )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithTag( Constants.EntityTag )
			.Run();

		if ( !trace.Hit )
		{
			return;
		}

		var pickupObj = trace.GameObject.Root;
		if ( !pickupObj.IsValid() || !pickupObj.Tags.Has( Constants.PocketItemTag ) || !GameUtils.HasPermission( caller, pickupObj ) )
		{
			player.Error( "#notify.pocket.forbidden" );
			return;

		}

		if ( !Pockets.TryGetValue( player.SteamId, out var pocket ) )
		{
			pocket = new List<GameObject>();
			Pockets[player.SteamId] = pocket;
		}

		if ( pocket.Count >= Config.Current.Game.MaxPocketItems )
		{
			player.Error( "#notify.pocket.full" );
			return;
		}

		lock ( PickupLock )
		{
			if ( !pickupObj.Tags.Has( Constants.PocketTag ) )
			{
				pickupObj.Network.DropOwnership();
				pickupObj.Tags.Add( Constants.PocketTag );

				pickupObj.OnPlayerInteractHost( player );
			}
			else
			{
				return;
			}
		}

		pocket.Add( pickupObj );

		pickupObj.Enabled = false;
		pickupObj.Network.Refresh();

		SyncPocketCount( player, pocket.Count );
		var pickupName = pickupObj.Name.StartsWith( '#' ) ? Language.GetPhrase( pickupObj.Name[1..] ) : pickupObj.Name;
		_ = ServerApiClient.Audit( "PocketPickup", $"{player.SteamName} ({player.SteamId}) pocketed {pickupName}", player.SteamId );
		player.Success( string.Format( Language.GetPhrase( "notify.pocket.pickup" ), pickupName ) );
	}

	[Rpc.Host]
	public void DropHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:pocket", Config.Current.Game.PocketCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			return;
		}

		if ( !Pockets.TryGetValue( player.SteamId, out var pocket ) || pocket.Count == 0 )
		{
			player.Error( "#notify.pocket.drop.nothing" );
			return;
		}

		var item = pocket.Last();
		if ( !item.IsValid() )
		{
			Pockets[player.SteamId].Remove( item );
			return;
		}

		item.Tags.Remove( Constants.PocketTag );
		Pockets[player.SteamId].Remove( item );

		item.Enabled = true;
		item.WorldPosition = GameUtils.GetSpawnPosition( player.AimRay );

		// Activate motion TEMP
		var rb = item.GetComponent<Rigidbody>();
		if ( rb.IsValid() )
		{
			rb.MotionEnabled = true;
		}

		ResetItemDecay( item );

		item.Network.Refresh();

		item.Network.AssignOwnership( player.Connection );
		OcclusionSystem.Current?.BroadcastForceCheckHost( player.Connection );

		SyncPocketCount( player, pocket.Count );
		var itemName = item.Name.StartsWith( '#' ) ? Language.GetPhrase( item.Name[1..] ) : item.Name;
		_ = ServerApiClient.Audit( "PocketDrop", $"{player.SteamName} ({player.SteamId}) dropped from pocket {itemName}", player.SteamId );
		player.Success( string.Format( Language.GetPhrase( "notify.pocket.drop.success" ), itemName ) );
	}

	public void OnPlayerDisconnectHost( long steamId )
	{
		if ( !Pockets.TryGetValue( steamId, out var pocket ) )
		{
			return;
		}

		foreach ( var item in pocket )
		{
			item.Destroy();
		}

		Pockets.Remove( steamId );
	}

	public void OnPlayerJoined( Player player )
	{
		var count = Pockets.TryGetValue( player.SteamId, out var pocket ) ? pocket.Count : 0;
		SyncPocketCount( player, count );
	}

	public void OnPlayerJobChangedHost( Player player, GameModeJobDto before, GameModeJobDto after )
	{
		if ( !Config.Current.Game.DropPocketsOnJobChange || !Networking.IsHost || player.Restricted )
		{
			return;
		}

		DropPocket( player );
	}

	public void OnPlayerKillHost( Player player )
	{
		if ( !Config.Current.Game.DropPocketsOnDeath || !Networking.IsHost || player.Restricted )
		{
			return;
		}

		DropPocket( player );
	}

	private void DropPocket( Player player )
	{
		Assert.True( Networking.IsHost );

		if ( !Pockets.TryGetValue( player.SteamId, out var pocket ) || pocket.Count == 0 || player.Restricted )
		{
			return;
		}

		foreach ( var item in pocket )
		{
			if ( !item.IsValid() )
			{
				item.Destroy();
				continue;
			}

			item.Tags.Remove( Constants.PocketTag );
			item.WorldPosition = GameUtils.GetSpawnPosition( player.AimRay );
			item.Transform.ClearInterpolation();

			ResetItemDecay( item );

			item.Enabled = true;
			item.Network.Refresh();
		}

		Pockets.Remove( player.SteamId );
		SyncPocketCount( player, 0 );
	}

	public List<string> ListPocketItems( long steamId )
	{
		if ( !Pockets.TryGetValue( steamId, out var items ) || items.Count == 0 )
		{
			return new List<string>();
		}

		return items
			.Where( i => i.IsValid() )
			.Select( i => i.Name )
			.ToList();
	}

	public void SyncPocketCount( Player player, int count )
	{
		using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
		{
			UpdateLocalPocketCount( count );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void UpdateLocalPocketCount( int count )
	{
		LocalPocketCount = count;
	}

	private void ResetItemDecay( GameObject item )
	{
		var timedDestroy = item.GetComponent<TimedDestroyComponent>( true );

		if ( timedDestroy.IsValid() )
		{
			timedDestroy.ResetTimer();
		}
	}

	[ConCmd( "dx_pocket_list", ConVarFlags.Server )]
	public static void ListPocketItems( Connection caller )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.Noclip ) )
		{
			return;
		}

		foreach ( var (playerId, items) in Instance.Pockets )
		{
			var player = GameUtils.GetPlayerById( playerId );
			caller.SendLog( LogLevel.Info, $"Player {(player.IsValid() ? player.DisplayName : "Unknown")} ({playerId}) has {items.Count} items in pocket." );

			foreach ( var item in items )
			{
				caller.SendLog( LogLevel.Info, $"- Item: {item.Name} (ID: {item.Id})" );
			}
		}
	}
}
