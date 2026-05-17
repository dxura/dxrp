using Sandbox.Diagnostics;
using Sandbox.Services;

namespace Dxura.RP.Game;

public partial class Door
{
	public void OnHandLmb( Player player )
	{
		Knock();
	}

	public void OnHandRmb( Player player )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "door:lock", Config.Current.Game.DoorLockCooldown, true ) )
		{
			return;
		}

		var hasAccess = Owner != 0 && Owner == player.SteamId ||
		                Owner != 0 && FriendSystem.Instance.HasDoorPermission( Owner, player.SteamId ) ||
		                !string.IsNullOrWhiteSpace( OwnerGroupIdentifier ) && player.Job.IsInGroup( OwnerGroupIdentifier ) ||
		                !string.IsNullOrWhiteSpace( OwnerJobIdentifier ) && OwnerJob?.Id == player.Job.Id;

		if ( !hasAccess )
		{
			Knock();
			return;
		}

		Achievements.Unlock( "lock_door" );

		ToggleLock();
	}

	public void OnHandMmb( Player player )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "door:buysell", Config.Current.Game.DoorBuySellCooldown, true ) )
		{
			return;
		}

		if ( IsOwned )
		{
			if ( player.SteamId == Owner )
			{
				SellDoorHost();
			}
		}
		else
		{
			if ( Config.Current.Game.MoneyEnabled && player.WalletBalance + player.BankBalance < Price )
			{
				Notify.Error( "#notify.door.poor" );
				return;
			}

			Achievements.Unlock( "buy_door" );

			BuyDoorHost();
		}
	}

	public void OnDoorPressed()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "door:use", Config.Current.Game.DoorUseCooldown, true ) )
		{
			return;
		}

		OnUseHost();
	}

	[Rpc.Host]
	private void OnUseHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:door:use", Config.Current.Game.DoorUseCooldown ) )
		{
			return;
		}

		if ( IsAnimating )
		{
			return;
		}

		// LOS check to prevent remote toggling
		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( BreachSystem.IsBreached( this ) )
		{
			player.Error( "#notify.door.breached" );
			player.Info( "Wait " + BreachSystem.GetRemainingBreachTime( this ) + "s" );
			return;
		}

		// Prevent opening while being pried
		if ( GameObject.Root.Tags.Has( Constants.PryingTag ) )
		{
			player.Error( "#notify.door.being_pried" );
			return;
		}

		if ( !IsPlayerInReach( player ) )
		{
			return;
		}

		Toggle( player.WorldPosition );
	}

	public void Toggle( Vector3 userPosition, bool ignoreLocked = false )
	{
		Assert.True( Networking.IsHost );

		if ( BreachSystem.IsBreached( this ) )
		{
			return;
		}

		if ( HasHandles )
		{
			AnimateHandles();
		}

		if ( Locked && !ignoreLocked )
		{
			LockedSound.Broadcast( WorldPosition );
			return;
		}

		var newState = State == DoorState.Closed ? DoorState.Open : DoorState.Closed;

		if ( newState == DoorState.Open && OpenAwayFromPlayer && Type != DoorType.Roller )
		{
			var doorToPlayer = (userPosition - WorldPosition).Normal;
			var doorForward = Transform.Local.Rotation.Forward;

			ReverseDirection = Vector3.Dot( doorToPlayer, doorForward ) > 0;
		}

		BroadcastState( newState, ReverseDirection );

		if ( newState == DoorState.Open )
		{
			OpenSound.Broadcast( WorldPosition );
		}
		else
		{
			CloseSound.Broadcast( WorldPosition );
		}
	}

	private void Knock()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "door:knock", Config.Current.Game.DoorKnockCooldown ) )
		{
			return;
		}

		Achievements.Unlock( "knock_door" );

		KnockHost();
	}

	[Rpc.Host]
	private void KnockHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:door:knock", Config.Current.Game.DoorKnockCooldown ) )
		{
			return;
		}

		KnockSound.BroadcastHost( WorldPosition );
	}
}
