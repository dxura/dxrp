using Dxura.RP.Game.UI;

namespace Dxura.RP.Game;

public partial class Player
{
	/// <summary>
	///     An accessor for health component if we have one.
	/// </summary>
	[Property]
	[Feature( "Misc" )]
	[RequireComponent]
	public HealthComponent HealthComponent { get; set; } = null!;

	/// <summary>
	///     The player's health component
	/// </summary>
	[RequireComponent]
	public required ArmorComponent ArmorComponent { get; set; }

	/// <summary>
	///	 State of being dead
	/// </summary>
	public bool IsDead => HealthComponent.State != LifeState.Alive;

	private RealTimeSince _timeSinceDamageTaken = 1;

	void IAreaDamageReceiver.ApplyAreaDamage( AreaDamage component )
	{
		var dmg = new DamageInfo( component.Attacker, component.Damage, component.Inflictor,
			WorldPosition,
			(WorldPosition - component.WorldPosition).Normal * Math.Min( component.Damage, 30 ) * 200f,
			Flags: component.DamageFlags );

		HealthComponent.TakeDamageHost( dmg );
	}

	void IDamageEvents.OnDamageGivenHost( Component attacker, DamageInfo damageInfo )
	{
		// Did we cause this damage?
		if ( IsLocalPlayer )
		{
			Crosshair.Instance.Trigger( damageInfo );
		}
	}

	void IDamageEvents.OnModifyDamageTaken( Component victim, ref DamageInfo damageInfo )
	{
		// Party protection runs first: friendly-fire between party members is blocked entirely regardless
		// of governance role (e.g. two police in the same party still can't hurt each other).
		if ( ApplyPartyDamageProtection( ref damageInfo ) )
		{
			return;
		}

		if ( ApplyGovernanceDamageMultipliers( ref damageInfo ) )
		{
			return;
		}

		var multiplier = Status.Current.ModifyDamageTaken( this );
		if ( multiplier < 1f )
		{
			damageInfo = damageInfo with { Damage = damageInfo.Damage * multiplier };
		}
	}

	/// <summary>
	///     Blocks damage between members of the same party when the party system has friendly fire
	///     disabled (<see cref="PartySystemConfig.PreventPartyDamage" />). Self-damage is never affected.
	///     Returns true if the damage was fully cleared.
	/// </summary>
	private bool ApplyPartyDamageProtection( ref DamageInfo damageInfo )
	{
		var party = PartySystem.Instance;
		if ( party is null || !party.Settings.PreventPartyDamage )
		{
			return false;
		}

		var attacker = GameUtils.GetPlayerFromComponent( damageInfo.Attacker );
		if ( !attacker.IsValid() || attacker == this )
		{
			return false;
		}

		if ( !party.AreInSameParty( attacker.SteamId, SteamId ) )
		{
			return false;
		}

		DamageExtensions.ClearDamage( ref damageInfo );
		return true;
	}

	private bool ApplyGovernanceDamageMultipliers( ref DamageInfo damageInfo )
	{
		var attackerPlayer = GameUtils.GetPlayerFromComponent( damageInfo.Attacker );
		if ( !attackerPlayer.IsValid() )
		{
			return false;
		}

		if ( Job.IsMayoralRole() && attackerPlayer.Job.IsPoliceRole() )
		{
			return ApplyDamageMultiplier( ref damageInfo, Config.Current.Game.GovernanceMayorPoliceDamageMultiplier );
		}

		if ( Job.IsPoliceRole() && attackerPlayer.Job.IsPoliceRole() )
		{
			return ApplyDamageMultiplier( ref damageInfo, Config.Current.Game.GovernancePoliceDamageMultiplier );
		}

		return false;
	}

	private static bool ApplyDamageMultiplier( ref DamageInfo damageInfo, float damageMultiplier )
	{
		var multiplier = MathF.Max( 0f, damageMultiplier );
		if ( multiplier <= 0f )
		{
			DamageExtensions.ClearDamage( ref damageInfo );
			return true;
		}

		if ( multiplier != 1f )
		{
			DamageExtensions.ScaleDamage( ref damageInfo, multiplier );
		}

		return false;
	}

	void IDamageEvents.OnDamageTakenHost( Component victim, DamageInfo damageInfo )
	{
		_timeSinceDamageTaken = 0;

		var attacker = GameUtils.GetPlayerFromComponent( damageInfo.Attacker );
		var playerVictim = GameUtils.GetPlayerFromComponent( damageInfo.Victim );

		var position = damageInfo.Position;
		var force = damageInfo.Force.IsNearZeroLength ? Random.Shared.VectorInSphere() : damageInfo.Force;

		AnimationHelper?.ProceduralHitReaction( damageInfo.Damage / 100f, force );

		if ( !damageInfo.Attacker.IsValid() )
		{
			return;
		}

		// Is this the local player?
		if ( IsLocalPlayer )
		{
			DamageIndicator.Current.OnHit( position );
		}

		if ( attacker != victim )
		{
			DamageTakenPosition = position;
			DamageTakenForce = force;
		}

		// Handle hit effects
		if ( damageInfo.Hitbox.HasFlag( HitboxTags.Head ) )
		{
			HandleHeadshotEffects( damageInfo, position, attacker, playerVictim );
		}
		else
		{
			HandleBodyshotEffects( position );
		}
	}

	public void KillHost()
	{
		HealthComponent.TakeDamageHost( new DamageInfo( this, float.MaxValue ) );
	}

	// Fall damage
	public void OnLanded( float distance, Vector3 impactVelocity )
	{
		if ( !IsLocalPlayer || Rigidbody.Velocity.Length < 1f )
		{
			return;
		}

		TakeFallDamageHost( distance, impactVelocity );
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void TakeFallDamageHost( float distance, Vector3 impactVelocity )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:damage:fall", Config.Current.Game.FallDamageCooldown ) )
		{
			return;
		}

		// Check if any status prevents fall damage (TODO: Improve)
		if ( Statuses.Keys.Any( x => Status.Current.GetCachedInstance( x )?.PreventFallDamage ?? false ) )
		{
			return;
		}

		// Don't take damage if we didn't fall far enough
		if ( distance < Config.Current.Game.FallDamageMinimumDistance )
		{
			return;
		}

		// Don't take fall damage if we recently teleported
		if ( _timeSinceLastTeleport < 1.5f )
		{
			return;
		}

		// Calculate damage based on fall distance
		// Scales from 0 damage at minimum distance to 100 damage at death distance
		var damageScale = distance.Remap( Config.Current.Game.FallDamageMinimumDistance, Config.Current.Game.FallDamageDeathDistance, 0f, 1f );
		damageScale = Math.Clamp( damageScale, 0f, 1f );

		// Apply a power curve to make damage ramp up more naturally (like Source engine games)
		damageScale = MathF.Pow( damageScale, 1.5f );

		var damageAmount = damageScale * 100 * Config.Current.Game.FallDamageMultiplier;
		if ( damageAmount < 1 )
		{
			return;
		}

		HealthComponent.TakeDamageHost(
			new DamageInfo( this, damageAmount, null, WorldPosition, Vector3.Down * impactVelocity.Length * 0.15f, Flags: DamageFlags.FallDamage ) );
	}

}
