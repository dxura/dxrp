using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public partial class Door : IOwned
{
	[Property]
	[ReadOnly]
	public long Owner { get; set; }

	[Property] public string? OwnerJobIdentifier { get; set; }
	[Property] public string? OwnerGroupIdentifier { get; set; }

	public GameModeJobDto? OwnerJob => string.IsNullOrWhiteSpace( OwnerJobIdentifier )
		? null
		: GameModeJobs.FindByReference( OwnerJobIdentifier );

	public GameModeJobGroupDto? OwnerGroup => string.IsNullOrWhiteSpace( OwnerGroupIdentifier )
		? null
		: GameModeJobs.FindGroupByReference( OwnerGroupIdentifier );

	[Property] public uint Price { get; set; } = 1000;

	public bool IsOwned => Owner != 0 || !string.IsNullOrWhiteSpace( OwnerGroupIdentifier ) || !string.IsNullOrWhiteSpace( OwnerJobIdentifier );

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastOwner( long owner )
	{
		Owner = owner;
	}

	public void ForceSell()
	{
		Assert.True( Networking.IsHost );

		BroadcastOwner( 0 );
		BroadcastLocked( false );
	}


	[Rpc.Host]
	private void SellDoorHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:door:buysell", Config.Current.Game.DoorBuySellCooldown ) )
		{
			return;
		}

		var callerPlayer = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !callerPlayer.IsValid() )
		{
			return;
		}

		if ( !IsOwned || callerPlayer.SteamId != Owner )
		{
			return;
		}

		if ( !IsPlayerInReach( callerPlayer ) )
		{
			return;
		}

		BroadcastOwner( 0 );
		BroadcastLocked( false );

		SellSound.Broadcast( WorldPosition );
	}

	[Rpc.Host]
	private void BuyDoorHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:door:buysell", Config.Current.Game.DoorBuySellCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( IsOwned || !player.IsValid() )
		{
			return;
		}

		if ( !IsPlayerInReach( player ) )
		{
			return;
		}

		var totalOwned = Scene.GetAllComponents<Door>()
			.Count( d => d.Owner == player.SteamId );

		if ( totalOwned >= Config.Current.Game.DoorLimit )
		{
			player.Error( "#notify.door.limit" );
			return;
		}

		_ = player.ChargeHost( Price, "Bought Door", true ).ContinueWith( async chargeResult =>
		{
			await GameTask.MainThread();

			if ( !player.IsValid() || !GameObject.IsValid() )
			{
				return;
			}

			if ( !chargeResult.Result )
			{
				return;
			}

			BroadcastOwner( player.SteamId );
			BuySound.Broadcast( WorldPosition );
		} );
	}
}
