namespace Dxura.RP.Game;

public partial class Door
{
	[Property]
	public bool Locked { get; set; }

	private void ToggleLock()
	{
		if ( Locked )
		{
			UnlockDoorHost();
		}
		else
		{
			LockDoorHost();
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastLocked( bool locked )
	{
		Locked = locked;
	}

	[Rpc.Host]
	private void LockDoorHost()
	{
		if ( !CanToggleLock() )
		{
			return;
		}

		BroadcastLocked( true );
		LockSound.Broadcast( WorldPosition );
	}

	[Rpc.Host]
	private void UnlockDoorHost()
	{
		if ( !CanToggleLock() )
		{
			return;
		}

		BroadcastLocked( false );
		UnlockSound.Broadcast( WorldPosition );
	}

	private bool CanToggleLock()
	{
		var callerId = Rpc.CallerId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:door:lock", Config.Current.Game.DoorLockCooldown ) )
		{
			return false;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return false;
		}

		if ( !IsPlayerInReach( player ) )
		{
			return false;
		}

		if ( Owner == player.SteamId )
		{
			return true;
		}

		if ( FriendSystem.Instance.HasDoorPermission( Owner, player.SteamId ) )
		{
			return true;
		}

		if ( !string.IsNullOrWhiteSpace( OwnerGroupIdentifier ) &&
		     !player.Job.IsInGroup( OwnerGroupIdentifier ) )
		{
			return true;
		}

		if ( !string.IsNullOrWhiteSpace( OwnerJobIdentifier ) &&
		     OwnerJob?.Id != player.Job.Id )
		{
			return true;
		}

		return false;
	}
}
