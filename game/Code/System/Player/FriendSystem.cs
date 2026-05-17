namespace Dxura.RP.Game;

public class FriendSystem : SingletonComponent<FriendSystem>, IGameEvents
{
	[Property]
	[Sync( SyncFlags.FromHost )]
	private NetDictionary<long, List<long>> ConstructPermissions { get; set; } = new();

	[Property]
	[Sync( SyncFlags.FromHost )]
	private NetDictionary<long, List<long>> DoorPermissions { get; set; } = new();

	public bool HasConstructPermission( long ownerId, long requesterId )
	{
		if ( ownerId == requesterId )
		{
			return true;
		}
		return ConstructPermissions.TryGetValue( ownerId, out var list ) && list.Contains( requesterId );
	}

	public bool HasDoorPermission( long ownerId, long requesterId )
	{
		if ( ownerId == requesterId )
		{
			return true;
		}
		return DoorPermissions.TryGetValue( ownerId, out var list ) && list.Contains( requesterId );
	}

	[Rpc.Host]
	public void AddConstructPermission( long targetId )
	{
		AddPermission( targetId, ConstructPermissions, "construct" );
	}

	[Rpc.Host]
	public void RemoveConstructPermission( long targetId )
	{
		RemovePermission( targetId, ConstructPermissions, "construct" );
	}

	[Rpc.Host]
	public void AddDoorPermission( long targetId )
	{
		AddPermission( targetId, DoorPermissions, "door" );
	}

	[Rpc.Host]
	public void RemoveDoorPermission( long targetId )
	{
		RemovePermission( targetId, DoorPermissions, "door" );
	}

	private void AddPermission( long targetId, NetDictionary<long, List<long>> permissions, string type )
	{
		var callerId = Rpc.CallerId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:{type}:add", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var callerPlayer = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !callerPlayer.IsValid() )
		{
			return;
		}

		if ( callerPlayer.SteamId == targetId || !GameUtils.GetPlayerById( targetId ).IsValid() )
		{
			return;
		}

		if ( !permissions.TryGetValue( callerPlayer.SteamId, out var list ) )
		{
			permissions[callerPlayer.SteamId] = new List<long>
			{
				targetId
			};
		}
		else if ( !list.Contains( targetId ) )
		{
			var newList = new List<long>( list )
			{
				targetId
			};
			permissions[callerPlayer.SteamId] = newList;
		}
	}

	public int GetPlayerFriendCount( long steamId )
	{
		var total = 0;

		if ( ConstructPermissions.TryGetValue( steamId, out var constructList ) )
		{
			total += constructList.Count;
		}

		if ( DoorPermissions.TryGetValue( steamId, out var doorList ) )
		{
			total += doorList.Count;
		}

		return total;
	}

	private void RemovePermission( long targetId, NetDictionary<long, List<long>> permissions, string type )
	{
		var callerId = Rpc.CallerId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:{type}:remove", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var callerPlayer = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !callerPlayer.IsValid() )
		{
			return;
		}

		if ( callerPlayer.SteamId == targetId || !GameUtils.GetPlayerById( targetId ).IsValid() )
		{
			return;
		}

		if ( permissions.TryGetValue( callerPlayer.SteamId, out var list ) && list.Contains( targetId ) )
		{
			var newList = new List<long>( list );
			newList.Remove( targetId );
			permissions[callerPlayer.SteamId] = newList;
		}
	}

	// Cleanup permissions when a player disconnects
	public void OnPlayerDisconnectHost( long disconnectSteamId )
	{
		ConstructPermissions.Remove( disconnectSteamId );
		DoorPermissions.Remove( disconnectSteamId );

		// Also remove this player from other players' permission lists
		foreach ( var playerId in ConstructPermissions.Keys.ToList() )
		{
			if ( ConstructPermissions.TryGetValue( playerId, out var propList ) && propList.Contains( disconnectSteamId ) )
			{
				var newList = new List<long>( propList );
				newList.Remove( disconnectSteamId );
				ConstructPermissions[playerId] = newList;
			}
		}

		foreach ( var playerId in DoorPermissions.Keys.ToList() )
		{
			if ( DoorPermissions.TryGetValue( playerId, out var doorList ) && doorList.Contains( disconnectSteamId ) )
			{
				var newList = new List<long>( doorList );
				newList.Remove( disconnectSteamId );
				DoorPermissions[playerId] = newList;
			}
		}
	}
}
