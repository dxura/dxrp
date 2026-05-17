using Dxura.RP.Game.UI;
using System.Threading.Tasks;

namespace Dxura.RP.Game.Equipments;

public class MedKitEquipment : InputWeaponComponent, IEquipmentEvents
{
	[Property] [Group( "Effects" )] private SoundEvent? HealSound { get; set; }
	[Property] [Group( "Effects" )] private SoundEvent? ReviveSound { get; set; }
	[Property] [Group( "Effects" )] private SoundEvent? RevivePumpSound { get; set; }
	[Property] [Group( "Effects" )] private SoundEvent? GruntSound { get; set; }
	[Property] [Group( "Effects" )] private SoundEvent? ReviveFailSound { get; set; }

	private bool _isHealing;
	private bool _inMinigame;
	private Player? _targetPlayer;
	private HealthComponent? _targetHealth;
	private DeadBody? _cachedDeadBody;
	private Guid? _cachedDeadBodyId;
	private Vector3 _cachedDeadBodyStartPosition;


	protected override void OnInput()
	{
		if ( _inMinigame )
		{
			return;
		}

		var healSelf = Input.Down( "Attack2" );
		var target = GetTarget( healSelf );

		if ( target == null )
		{
			Stop();
			return;
		}

		if ( target.Value.health.State == LifeState.Dead && !healSelf )
		{
			return; // Dead targets handled in OnInputDown
		}

		StartHealing( target.Value, healSelf );
	}

	protected override void OnInputDown()
	{
		if ( _inMinigame )
		{
			return;
		}

		var healSelf = Input.Down( "Attack2" );
		var wantsRevive = Input.Down( "Attack1" ) && !healSelf;

		if ( !wantsRevive )
		{
			return;
		}

		var target = GetTarget( false );
		if ( target?.health.State != LifeState.Dead )
		{
			return;
		}

		StartRevive( target.Value );
	}

	protected override void OnInputUp()
	{
		if ( _inMinigame )
		{
			if ( Input.Released( "Attack1" ) )
			{
				FailRevive( CompressionMinigame.FailReason.ButtonReleased );
			}
			return;
		}

		if ( Input.Released( "Attack1" ) || Input.Released( "Attack2" ) )
		{
			Stop();
		}
	}

	protected override void OnInputFixedUpdate()
	{
		if ( _inMinigame )
		{
			HandleMinigame();
			return;
		}

		if ( _isHealing )
		{
			UpdateHealing();
		}
	}

	protected override void OnDisabled()
	{
		Stop();
	}
	public new void OnEquipmentHolstered( Equipment equipment )
	{
		Stop();
	}
	public void OnEquipmentDestroyed( Equipment equipment )
	{
		Stop();
	}

	private (Player? player, HealthComponent health)? GetTarget( bool healSelf )
	{
		if ( healSelf )
		{
			return (Player.Local, Player.Local.HealthComponent);
		}

		var trace = GetTrace();

		if ( !trace.HasValue )
		{
			return null;
		}

		var root = trace.Value.GameObject?.Root;

		if ( root?.GetComponent<DeadBody>() is var deadBody && deadBody.IsValid() )
		{
			if ( root.WorldPosition.Distance( GetInteractionPosition() ) > GetMaxReviveDistance() )
			{
				return null;
			}

			return (deadBody.Player, deadBody.Player.HealthComponent);
		}

		// Healing should be a personal space activity
		if ( root.IsValid() && root.WorldPosition.Distance( GetInteractionPosition() ) > Config.Current.Game.ReachDistance * 0.5f )
		{
			return null;
		}

		if ( root.IsValid() && root.GetComponent<HealthComponent>() is var health && health.IsValid() )
		{
			return (root?.GetComponent<Player>(), health);
		}

		return null;
	}

	private void StartHealing( (Player? player, HealthComponent health) target, bool healSelf )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "heal", Config.Current.Game.HealCooldown ) )
		{
			return;
		}
		if ( target.health.Health >= target.health.MaxHealth * 1.25 )
		{
			return;
		}

		// Only allow healing self or others who are not dead
		if ( Config.Current.Game.MedkitHealOnlyAllowHealingPlayers && !target.player.IsValid() )
		{
			return;
		}

		_isHealing = true;
		_targetHealth = target.health;

		var root = target.health.GameObject.Root;
		var name = target.player?.DisplayName ??
		           root.GetComponent<IDescription>()?.DisplayName ??
		           "Target";
		var progress = target.health.Health / (target.health.MaxHealth * 1.25);

		ShowMessage( $"Healing {name}", (float)progress );
		HealSound?.Broadcast( target.health.WorldPosition, target.health.GameObject );
		GameManager.Instance.HealHost( healSelf );
	}

	private void StartRevive( (Player? player, HealthComponent health) target )
	{
		if ( !CanRevive( target.player ) )
		{
			return;
		}

		_inMinigame = true;
		_targetPlayer = target.player;
		_cachedDeadBody = Scene.GetAllComponents<DeadBody>().FirstOrDefault( d => d.Player == target.player );
		_cachedDeadBodyId = _cachedDeadBody?.Id;
		_cachedDeadBodyStartPosition = _cachedDeadBody?.WorldPosition ?? GetInteractionPosition();

		var medic = GameUtils.GetPlayerByConnectionId( Connection.Local.Id );
		medic?.SetSit( SitType.KneelRevive );

		_ = StartMinigameWhenReady( target.player );

		if ( target.player?.GameObject.IsValid() == true )
		{
			StartReviveHost( target.player.SteamId );
		}
	}

	private Vector3 GetInteractionPosition()
	{
		return Player?.WorldPosition ?? WorldPosition;
	}

	private float GetMaxReviveDistance()
	{
		return Config.Current.Game.ReachDistance + Config.Current.Game.ReviveDistanceBuffer;
	}

	private bool CanRevive( Player? target )
	{
		if ( Cooldown.Current.IsOnCooldown( "revive" ) )
		{
			Notify.Cooldown( "revive" );
			return false;
		}

		var medic = GameUtils.GetPlayerByConnectionId( Connection.Local.Id );
		if ( medic == null || !medic.Job.IsMedicRole() )
		{
			medic?.Error( "Only medics can revive players" );
			return false;
		}

		if ( !target.IsValid() )
		{
			return false;
		}

		if ( target.RespawnState != RespawnState.Delayed )
		{
			medic?.Error( "This player cannot be revived" );
			return false;
		}


		return true;
	}

	private void HandleMinigame()
	{
		if ( Input.Pressed( "attack2" ) )
		{
			CompressionMinigame.Instance?.TriggerCompression();
		}


		if ( _targetPlayer.IsValid() )
		{
			if ( _targetPlayer.RespawnState == RespawnState.Immediate )
			{
				FailRevive( CompressionMinigame.FailReason.TargetRespawned );
				return;
			}

			// Use the body's position at revive start: the medic is locked in kneel mode and
			// can't drift, but the corpse is a ragdoll and can be shoved by players walking
			// past, which would otherwise spuriously fail the revive.
			var distance = _cachedDeadBodyStartPosition.Distance( GetInteractionPosition() );
			if ( distance > GetMaxReviveDistance() )
			{
				FailRevive( CompressionMinigame.FailReason.MovedTooFar );
			}
		}
		else
		{
			FailRevive( CompressionMinigame.FailReason.TargetRespawned );
		}
	}

	private void UpdateHealing()
	{
		if ( !_targetHealth.IsValid() || _targetHealth.Health >= _targetHealth.MaxHealth * 1.25 )
		{
			Stop();
			return;
		}

		var root = _targetHealth.GameObject.Root;
		var name = root.GetComponent<Player>()?.DisplayName ??
		           root.GetComponent<IDescription>()?.DisplayName ??
		           "Target";
		var progress = (float)(_targetHealth.Health / (_targetHealth.MaxHealth * 1.25));
		ShowMessage( $"Healing {name}", progress );
	}

	private void OnMinigameComplete( bool success, CompressionMinigame.FailReason failReason = CompressionMinigame.FailReason.None )
	{
		if ( success && _targetPlayer.IsValid() )
		{
			GruntSound?.Broadcast( WorldPosition, GameObject );
			RevivePlayerHost( _targetPlayer.GameObject, _cachedDeadBodyId );
			Cooldown.Current.StartCooldown( "revive", Config.Current.Game.ReviveCooldown );
		}
		else
		{
			FailRevive( failReason );
		}

		Stop();
	}

	private void OnCompressionAttempt( CompressionMinigame.HitResult result )
	{
		if ( result == CompressionMinigame.HitResult.Perfect )
		{
			RevivePumpSound?.Broadcast( WorldPosition, GameObject );
		}

		var kneelMode = MoveModeKneel.ActiveInstance;
		if ( kneelMode != null )
		{
			kneelMode.StartCompression();
			_ = EndCompressionAfterDelay( kneelMode );
		}
	}

	private async Task StartMinigameWhenReady( Player? targetPlayer )
	{
		var timeout = 2.0f;
		var elapsed = 0f;

		while ( MoveModeKneel.ActiveInstance == null && elapsed < timeout )
		{
			await GameTask.Delay( 250 );
			elapsed += 0.25f;
		}

		var name = targetPlayer?.DisplayName ?? "Target";
		CompressionMinigame.Instance.OnMinigameComplete = OnMinigameComplete;
		CompressionMinigame.Instance.OnCompressionAttempt = OnCompressionAttempt;
		CompressionMinigame.Instance.StartMinigame( $"Reviving {name}", Config.Current.Game.ReviveRequiredHits, Config.Current.Game.ReviveTimeLimit, targetPlayer, true );
	}

	private async Task EndCompressionAfterDelay( MoveModeKneel kneelMode )
	{
		await GameTask.Delay( 300 );
		kneelMode?.EndCompression();
	}

	private void FailRevive( CompressionMinigame.FailReason reason = CompressionMinigame.FailReason.None )
	{
		if ( !_targetPlayer.IsValid() )
		{
			return;
		}

		CancelReviveHost( _targetPlayer.SteamId );

		Cooldown.Current.StartCooldown( "revive", Config.Current.Game.ReviveCooldown );

		var medic = GameUtils.GetPlayerByConnectionId( Connection.Local.Id );
		medic?.SetSit( null );

		var message = GetFailMessage( reason );
		Notify.Warn( message );
		ReviveFailSound?.Broadcast( WorldPosition, GameObject );
		Stop();
	}

	private string GetFailMessage( CompressionMinigame.FailReason reason )
	{
		return reason switch
		{
			CompressionMinigame.FailReason.OutOfTime => "#equipment.medkit.revive.fail.time",
			CompressionMinigame.FailReason.ButtonReleased => "#equipment.medkit.revive.fail.released",
			CompressionMinigame.FailReason.MissedCompression => "#equipment.medkit.revive.fail.missed",
			CompressionMinigame.FailReason.MovedTooFar => "#equipment.medkit.revive.fail.toofar",
			CompressionMinigame.FailReason.TargetRespawned => "#equipment.medkit.revive.fail.respawned",
			_ => "#equipment.medkit.revive.fail.generic"
		};
	}

	private void Stop()
	{
		_isHealing = false;
		_inMinigame = false;
		_targetPlayer = null;
		_targetHealth = null;
		_cachedDeadBody = null;
		_cachedDeadBodyId = null;
		_cachedDeadBodyStartPosition = Vector3.Zero;

		if ( EquipmentOverlay.Instance.IsValid() )
		{
			EquipmentOverlay.Instance.IsActive = false;
		}
		if ( CompressionMinigame.Instance.IsValid() )
		{
			CompressionMinigame.Instance.StopMinigame();
		}

		var localPlayer = Player.Local;

		// Only make the player stand up if the medkit being stopped belongs to them.
		if ( localPlayer.IsValid() && Equipment.Owner == localPlayer )
		{
			localPlayer.SetSit( null );
		}
	}

	private void ShowMessage( string text, float progress = 0f )
	{
		EquipmentOverlay.Instance.Status = text;
		EquipmentOverlay.Instance.Progress = progress;
		EquipmentOverlay.Instance.IsActive = true;
	}

	// Simple, secure RPC methods
	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void StartReviveHost( long steamId )
	{
		var callerId = Rpc.CallerId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"revive:start:{callerId}", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var medic = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !medic.IsValid() )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"revive:{steamId}", Config.Current.Game.ReviveCooldown ) )
		{
			medic.Error( "Player has recently been revived" );
			return;
		}

		var target = GameUtils.GetPlayerById( steamId );

		if ( !IsValidRevive( medic, target ) || !target.IsValid() )
		{
			return;
		}

		var deadBody = Scene.GetAllComponents<DeadBody>().FirstOrDefault( d => d.Player == target );
		var bodyPosition = deadBody?.WorldPosition ?? target.WorldPosition;
		var maxDistance = Config.Current.Game.ReachDistance + Config.Current.Game.ReviveDistanceBuffer;

		if ( medic.WorldPosition.Distance( bodyPosition ) > maxDistance )
		{
			return;
		}

		ReviveSound?.Broadcast( medic.WorldPosition, medic.GameObject );
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void CancelReviveHost( long steamId )
	{
		var target = GameUtils.GetPlayerById( steamId );
		if ( !target.IsValid() )
		{
			return;
		}

		var medic = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );

	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void RevivePlayerHost( GameObject targetObject, Guid? deadBodyId )
	{
		var medic = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		var target = targetObject.GetComponent<Player>();

		if ( !IsValidRevive( medic, target ) || !medic.IsValid() )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"revived:{target.SteamId}", Config.Current.Game.ReviveCooldown ) )
		{
			return;
		}

		// Find dead body for position using cached ID first, fallback to player search
		var deadBody = deadBodyId.HasValue
			? Scene.GetAllComponents<DeadBody>().FirstOrDefault( d => d.Id == deadBodyId )
			: Scene.GetAllComponents<DeadBody>().FirstOrDefault( d => d.Player == target );

		var position = deadBody?.WorldPosition ?? target.WorldPosition;
		var maxDistance = Config.Current.Game.ReachDistance + Config.Current.Game.ReviveDistanceBuffer;

		if ( medic.WorldPosition.Distance( position ) > maxDistance )
		{
			return;
		}

		target.SpawnHost();
		target.TeleportHost( new Transform( position, Rotation.Identity ) );

		target.HealthComponent.Health = Config.Current.Game.ReviveHealthAmount;
		target.HealthComponent.IsGodMode = false;
		medic.IncrementStat( "revive", 1 );
	}

	private bool IsValidRevive( Player? medic, Player? target )
	{
		return medic.IsValid() &&
		       target.IsValid() &&
		       medic.Job.IsMedicRole() &&
		       target.HealthComponent.State == LifeState.Dead;
	}
}
