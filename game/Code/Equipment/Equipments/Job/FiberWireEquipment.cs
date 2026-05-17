using Dxura.RP.Game.UI;
using Sandbox.Services;
using System;

namespace Dxura.RP.Game.Equipments;

public class FiberWireEquipment : InputWeaponComponent, IEquipmentEvents
{
	[Property] [Group( "Effects" )] private SoundEvent? SnapSound { get; set; }
	[Property] [Group( "Effects" )] private SoundEvent? SnapMissSound { get; set; }


	protected override void OnInputDown()
	{
		if ( !Player.Local.IsValid() )
		{
			return;
		}

		var trace = GetTrace( Config.Current.Game.ReachDistance );

		if ( trace is not { Hit: true } || !trace.Value.GameObject.IsValid() )
		{
			return;
		}

		var target = trace.Value.GameObject.Root;
		if ( !target.IsValid() || !target.Tags.Has( Constants.PlayerTag ) )
		{
			return;
		}

		var targetPlayer = target.GetComponent<Player>();
		if ( !targetPlayer.IsValid() )
		{
			return;
		}

		if ( Cooldown.Current?.CheckAndStartCooldown( "snap", Config.Current.Game.ActionQuickCooldown, true ) == true )
		{
			return;
		}

		UseFiberWire( targetPlayer.SteamId );
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void UseFiberWire( long targetSteamId )
	{
		var caller = Rpc.Caller;
		var callerId = Rpc.CallerId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:snap", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var callerPlayer = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !callerPlayer.IsValid() )
		{
			return;
		}

		var targetPlayer = GameUtils.GetPlayerById( targetSteamId );
		if ( !targetPlayer.IsValid() )
		{
			return;
		}

		var activeHit = HitSystem.Instance.GetActiveHitForHitman( callerPlayer.SteamId );

		var isTarget = activeHit.HasValue && activeHit.Value.TargetSteamId == targetPlayer.SteamId;

		if ( !isTarget )
		{
			SnapMissSound?.BroadcastHost( WorldPosition );
			callerPlayer.Error( "Fiber Wire can only be used on your current target." );
			return;
		}

		// Check if caller is close enough to target
		var distance = Vector3.DistanceBetween( callerPlayer.WorldPosition, targetPlayer.WorldPosition );
		if ( distance > 40f ) // Must be very close
		{
			SnapMissSound?.BroadcastHost( WorldPosition );
			return;
		}

		// Check if caller is behind the target
		var directionToTarget = (targetPlayer.WorldPosition - callerPlayer.WorldPosition).Normal;
		var targetForward = targetPlayer.AimRay.Forward;
		var dotProduct = Vector3.Dot( directionToTarget, targetForward );

		// Caller must be behind target (dot product > 0 means same direction, i.e., behind)
		if ( dotProduct < 0.6f )
		{
			SnapMissSound?.BroadcastHost( WorldPosition );
			return;
		}

		targetPlayer.GameObject.TakeDamageHost( new DamageInfo(
			callerPlayer,
			1000f,
			callerPlayer.CurrentEquipment,
			targetPlayer.WorldPosition,
			Vector3.Zero
		) );

		SnapSound?.BroadcastHost( WorldPosition );
	}
}
