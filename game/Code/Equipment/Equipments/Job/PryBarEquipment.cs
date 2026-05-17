using Dxura.RP.Game.UI;
using Sandbox.Services;
using System;

namespace Dxura.RP.Game.Equipments;

public class PryBarEquipment : InputWeaponComponent, IEquipmentEvents
{
	[Property] [Group( "Effects" )] private SoundEvent? PryingSound { get; set; }

	[Sync( SyncFlags.FromHost )]
	private bool IsPrying { get; set; }

	[Sync( SyncFlags.FromHost )]
	private TimeSince PryStartTime { get; set; }

	[Sync( SyncFlags.FromHost )]
	private GameObject? PryTarget { get; set; }

	[Sync( SyncFlags.FromHost )]
	private bool IsRepairing { get; set; }

	private string _targetName = "";
	private float _localPryStartTime;

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
		var (isValid, targetName, reason) = ValidateTarget( target );

		if ( targetName == "" )
		{
			return;
		}

		if ( Cooldown.Current?.CheckAndStartCooldown( "pry", Config.Current.Game.PryCooldown, true ) == true )
		{
			return;
		}

		if ( !isValid )
		{
			Notify.Error( reason );
			return;
		}

		_localPryStartTime = Time.Now;
		StartPryHost( target );

		_targetName = targetName;

		if ( EquipmentOverlay.Instance.IsValid() )
		{
			EquipmentOverlay.Instance.Status = string.Format( Language.GetPhrase( "equipment.prybar.status.prying" ), targetName );
			EquipmentOverlay.Instance.Progress = 0;
			EquipmentOverlay.Instance.IsActive = true;
		}
	}

	protected override void OnInputUp()
	{
		if ( IsPrying )
		{
			CancelPrying();
		}
		else
		{
			if ( EquipmentOverlay.Instance.IsValid() )
			{
				EquipmentOverlay.Instance.IsActive = false;
			}
		}
	}

	protected override void OnInputFixedUpdate()
	{
		if ( !IsPrying || !PryTarget.IsValid() )
		{
			return;
		}

		var player = Player.Local;
		if ( !player.IsValid() || player.IsDead )
		{
			CancelPrying();
			return;
		}

		var distanceToTarget = CalculateDistance( player, PryTarget );

		if ( distanceToTarget > Config.Current.Game.PryMaxDistance )
		{
			if ( !Cooldown.Current.IsOnCooldown( "pry:toofar" ) )
			{
				Notify.Warn( "#notify.prybar.toofar" );
				Cooldown.Current.StartCooldown( "pry:toofar", 1.5f );
			}

			CancelPrying();
			return;
		}

		if ( !Cooldown.Current.CheckAndStartCooldown( "pry:effects", Config.Current.Game.PryEffectsCooldown ) )
		{
			DoPryEffectsHost();
		}

		var duration = IsRepairing ? Config.Current.Game.RepairDuration : Config.Current.Game.PryDuration;
		var statusText = IsRepairing
			? string.Format( Language.GetPhrase( "equipment.prybar.status.repairing" ), _targetName )
			: string.Format( Language.GetPhrase( "equipment.prybar.status.prying" ), _targetName );

		var elapsed = Time.Now - _localPryStartTime;
		var progress = Math.Clamp( elapsed / duration, 0, 1 );

		if ( EquipmentOverlay.Instance.IsValid() )
		{
			EquipmentOverlay.Instance.Status = statusText;
			EquipmentOverlay.Instance.Progress = progress;
			EquipmentOverlay.Instance.IsActive = true;
		}

		if ( PryStartTime >= duration )
		{
			CompletePryHost();
			CompletePrying();
		}
	}

	private float CalculateDistance( Player player, GameObject target )
	{
		var renderer = target.GetComponent<ModelRenderer>();

		if ( renderer.IsValid() && renderer.Model.IsValid() )
		{
			var bounds = renderer.Bounds;
			var closestPoint = bounds.ClosestPoint( player.WorldPosition );
			return player.WorldPosition.Distance( closestPoint );
		}

		return player.WorldPosition.Distance( target.WorldPosition );
	}

	private (bool isValid, string targetName, string reason) ValidateTarget( GameObject target )
	{
		if ( !target.IsValid() )
		{
			return (false, "", "");
		}

		var door = target.GetComponentInParent<Door>();
		if ( door.IsValid() )
		{
			if ( string.Equals( door.OwnerGroupIdentifier, "public", StringComparison.OrdinalIgnoreCase ) )
			{
				return (false, Language.GetPhrase( "roleplay.door.name" ), "#notify.prybar.door_public");
			}

			if ( string.IsNullOrWhiteSpace( door.OwnerJobIdentifier ) &&
			     string.IsNullOrWhiteSpace( door.OwnerGroupIdentifier ) &&
			     door.Owner == 0 )
			{
				return (false, Language.GetPhrase( "roleplay.door.name" ), "#notify.prybar.door_unowned");
			}

			return (true, Language.GetPhrase( "roleplay.door.name" ), "");
		}

		var prop = target.GetComponentInParent<Prop>();
		if ( prop.IsValid() && prop.FadingDoor )
		{
			return (true, Language.GetPhrase( "tool.fadingdoor.name" ), "");
		}

		return (false, "", "");
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void StartPryHost( GameObject target )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:pry", Config.Current.Game.PryCooldown ) )
		{
			return;
		}

		if ( !target.IsValid() )
		{
			return;
		}

		var door = target.GetComponentInParent<Door>();
		var prop = target.GetComponentInParent<Prop>();

		var isRepairing = false;

		if ( door.IsValid() )
		{
			isRepairing = BreachSystem.IsBreached( door );
		}
		else if ( prop.IsValid() && prop.FadingDoor )
		{
			isRepairing = BreachSystem.IsBreached( prop );
		}
		else
		{
			return;
		}

		IsPrying = true;
		IsRepairing = isRepairing;
		PryStartTime = 0;
		PryTarget = target;

		GameManager.Instance.BroadcastTagHost( target, true, Constants.PryingTag );
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void CompletePryHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:action", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !IsPrying )
		{
			return;
		}

		if ( !player.IsValid() || player.IsDead )
		{
			ClearPryState();
			return;
		}

		var target = PryTarget;

		if ( !target.IsValid() )
		{
			ClearPryState();
			return;
		}

		// Anti-cheat: Validate timing
		var expectedDuration = IsRepairing ? Config.Current.Game.RepairDuration : Config.Current.Game.PryDuration;
		if ( PryStartTime < expectedDuration - 0.5f ) // Allow 0.5s tolerance for network latency
		{
			Log.Warning( $"Player {player.SteamId} attempted to complete pry too early ({PryStartTime}s < {expectedDuration}s)" );
			ClearPryState();
			return;
		}

		// Anti-cheat: Validate distance
		var distance = CalculateDistance( player, target );
		if ( distance > Config.Current.Game.PryMaxDistance + 50f ) // Allow 50 units tolerance
		{
			Log.Warning( $"Player {player.SteamId} attempted to complete pry from too far away ({distance} > {Config.Current.Game.PryMaxDistance})" );
			ClearPryState();
			return;
		}

		player.IncrementStat( "pry", 1 );

		var door = target.GetComponentInParent<Door>();
		if ( door.IsValid() )
		{
			if ( IsRepairing )
			{
				if ( !BreachSystem.Instance.IsValid() )
				{
					ClearPryState();
					return;
				}

				BreachSystem.Instance.Repair( door );
				player.Success( "#notify.prybar.success_repair" );
				player.IncrementStat( "pry-repair", 1 );
			}
			else
			{
				if ( !BreachSystem.Instance.IsValid() )
				{
					ClearPryState();
					return;
				}

				BreachSystem.Instance.Breach( door, player.WorldPosition );
				player.Success( "#notify.prybar.success_door" );
				player.IncrementStat( "pry-door", 1 );
			}
			ClearPryState();
			return;
		}

		var prop = target.GetComponentInParent<Prop>();
		if ( prop.IsValid() )
		{
			if ( IsRepairing )
			{
				if ( !BreachSystem.Instance.IsValid() || !Config.Current.Game.FadingDoorRepairEnabled )
				{
					ClearPryState();
					return;
				}

				BreachSystem.Instance.Repair( prop );
				player.Success( "#notify.prybar.success_repair" );
				player.IncrementStat( "pry-repair", 1 );
			}
			else
			{
				if ( !BreachSystem.Instance.IsValid() )
				{
					ClearPryState();
					return;
				}

				BreachSystem.Instance.Breach( prop, player.WorldPosition );
				player.Success( "#notify.prybar.success_fading" );
				player.IncrementStat( "pry-fade", 1 );
			}
		}

		ClearPryState();
	}


	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void DoPryEffectsHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:pry:effects", Config.Current.Game.PryEffectsCooldown ) )
		{
			return;
		}

		BroadcastPryEffects();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void BroadcastPryEffects()
	{
		if ( !PryTarget.IsValid() )
		{
			return;
		}

		PryingSound?.Play( PryTarget.WorldPosition );
		Equipment?.Owner?.Renderer?.Set( "b_attack", true );
		Equipment?.ViewModel?.ModelRenderer?.Set( "b_attack", true );
	}

	private void CancelPrying()
	{
		if ( IsProxy && !Networking.IsHost )
		{
			return;
		}

		if ( Networking.IsHost )
		{
			ClearPryState();
		}
		else
		{
			CancelPryHost();
		}

		CompletePrying();
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void CancelPryHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:prybar:clear", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		ClearPryState();
	}

	private void ClearPryState()
	{
		if ( PryTarget.IsValid() )
		{
			GameManager.Instance.BroadcastTagHost( PryTarget, false, Constants.PryingTag );
		}

		IsPrying = false;
		IsRepairing = false;
		PryStartTime = 0;
		PryTarget = null;
	}

	private void CompletePrying()
	{
		if ( EquipmentOverlay.Instance.IsValid() )
		{
			EquipmentOverlay.Instance.Progress = 0;
			EquipmentOverlay.Instance.IsActive = false;
		}

		_targetName = "";
		_localPryStartTime = 0f;
	}

	protected override void OnDisabled()
	{
		CancelPrying();
	}

	public new void OnEquipmentHolstered( Equipment equipment )
	{
		CancelPrying();
	}

	public void OnEquipmentDestroyed( Equipment equipment )
	{
		CancelPrying();
	}
}
