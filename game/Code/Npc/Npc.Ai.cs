using DamageInfo=Dxura.RP.Game.DamageInfo;

namespace Dxura.RP.Game;

public partial class Npc
{
	[RequireComponent] public required NavMeshAgent Agent { get; set; }

	/// <summary>
	/// Current target for this NPC
	/// </summary>
	protected Player? CurrentTarget { get; set; }

	/// <summary>
	/// Last recorded position of the target
	/// </summary>
	protected Vector3 LastTargetPosition { get; set; }

	/// <summary>
	/// Time since the last attack was performed
	/// </summary>
	protected TimeSince LastAttackTime { get; set; }

	/// <summary>
	/// Time since the AI logic was last calculated
	/// </summary>
	protected TimeSince LastAiCalculationTime { get; set; }

	/// <summary>
	/// Time since the path was last updated
	/// </summary>
	protected TimeSince LastPathUpdateTime { get; set; }

	// Default AI configuration values - can be overridden in derived classes

	/// <summary>
	/// How often to update the AI decision making (in seconds)
	/// </summary>
	protected virtual float AiUpdateInterval => 0.1f;

	/// <summary>
	/// How often to update the movement path when chasing a target (in seconds)
	/// </summary>
	protected virtual float PathUpdateInterval => 0.5f;

	/// <summary>
	/// Distance at which NPC can attack the target
	/// </summary>
	protected virtual float AttackRange => 50f;

	/// <summary>
	/// Only update path if target moved at least this far
	/// </summary>
	protected virtual float PathUpdateThreshold => 100f;

	/// <summary>
	/// Distance considered "close range" for more frequent updates
	/// </summary>
	protected virtual float CloseRangeDistance => 200f;

	/// <summary>
	/// Update interval when target is at close range
	/// </summary>
	protected virtual float CloseRangeUpdateInterval => 0.1f;

	/// <summary>
	/// Movement threshold when target is at close range
	/// </summary>
	protected virtual float CloseRangeThreshold => 150f;

	/// <summary>
	/// Standard update interval when target is far away
	/// </summary>
	protected virtual float FarRangeUpdateInterval => 0.5f;

	/// <summary>
	/// Standard movement threshold when target is far away
	/// </summary>
	protected virtual float FarRangeThreshold => 100f;

	/// <summary>
	/// Cooldown between attacks (in seconds)
	/// </summary>
	protected virtual float AttackCooldown => 2.0f;

	/// <summary>
	/// Damage dealt per attack
	/// </summary>
	protected virtual float AttackDamage => 10f;

	/// <summary>
	/// Finds the nearest target or returns a specified override target
	/// </summary>
	/// <param name="overrideTarget">Optional override target</param>
	/// <returns>The selected target player</returns>
	protected virtual Player? FindTarget( Player? overrideTarget = null )
	{
		// If we have an override target, use it
		if ( overrideTarget.IsValid() )
		{
			return overrideTarget;
		}

		// Otherwise find the closest player
		return GameUtils.Players
			.OrderBy( x => WorldPosition.Distance( x.WorldPosition ) )
			.FirstOrDefault();
	}

	/// <summary>
	/// Updates movement path to chase the target
	/// </summary>
	protected virtual void UpdateMovement( GameObject target )
	{
		if ( !target.IsValid() )
		{
			return;
		}

		var distanceToTarget = WorldPosition.Distance( target.WorldPosition );
		var isCloseRange = distanceToTarget <= CloseRangeDistance;

		// Use different timing and thresholds based on distance
		var updateInterval = isCloseRange ? CloseRangeUpdateInterval : FarRangeUpdateInterval;
		var moveThreshold = isCloseRange ? CloseRangeThreshold : FarRangeThreshold;

		// Only update path if enough time has passed AND target has moved significantly
		if ( LastPathUpdateTime >= updateInterval &&
		     (LastTargetPosition - target.WorldPosition).Length >= moveThreshold )
		{
			LastPathUpdateTime = 0;
			LastTargetPosition = target.WorldPosition;
			Agent.MoveTo( target.WorldPosition );
		}
	}

	/// <summary>
	/// Stops the NPC from moving
	/// </summary>
	protected virtual void StopMoving()
	{
		Agent.Stop();
	}

	/// <summary>
	/// Attacks the target
	/// </summary>
	protected virtual void Attack( GameObject target )
	{
		if ( LastAttackTime < AttackCooldown )
		{
			return;
		}

		LastAttackTime = 0;


		// Apply damage to the target
		target.TakeDamageHost( new DamageInfo(
			this,
			AttackDamage,
			this,
			target.WorldPosition + new Vector3( 0, 0, 30 ),
			Hitbox: HitboxTags.Chest,
			Flags: DamageFlags.Melee
		) );

		// Trigger attack effects and animations
		OnAttackEffects();
		OnAttackAnimation();
	}

	protected virtual void OnUpdateAi() {}
	protected virtual void OnDamageAi( DamageInfo damageInfo ) {}

	/// <summary>
	/// Called when the NPC performs an attack
	/// </summary>
	protected virtual void OnAttackEffects() {}

	/// <summary>
	/// Called when the NPC performs an attack animation
	/// </summary>
	protected virtual void OnAttackAnimation() {}

	/// <summary>
	/// Override of DisableMovement from BaseNpc
	/// </summary>
	protected virtual void DisableMovement()
	{
		if ( Agent.IsValid() )
		{
			Agent.Enabled = false;
			StopMoving();
		}
	}
}
