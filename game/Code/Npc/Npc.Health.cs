namespace Dxura.RP.Game;

public partial class Npc
{
	[Property]
	public HealthComponent? Health { get; set; }

	public void OnKillHost( Component victim, DamageInfo damage )
	{
		Tags.Add( Constants.RagdollTag );
		Tags.Add( "dead" );
		BodyPhysics.Enabled = true;
		Collider.Enabled = false;

		OnKillEffects();

		// Disable AI movement
		DisableMovement();

		// Cleanup after time
		var timedDestroyComponent = AddComponent<TimedDestroyComponent>();
		timedDestroyComponent.Time = DestroyAfterDeathTime;

		// Reward killer
		RewardKiller( damage );
	}

	/// <summary>
	/// Rewards the player who killed this NPC
	/// </summary>
	protected virtual void RewardKiller( DamageInfo damageInfo )
	{
	}

	public void OnDamageTakenHost( Component victim, DamageInfo damage )
	{
		OnDamageEffects( damage );

		if ( !IsProxy )
		{
			OnDamageAi( damage );
		}
	}

	/// <summary>
	/// Handle area damage application
	/// </summary>
	public virtual void ApplyAreaDamage( AreaDamage component )
	{
		var dmg = new DamageInfo(
			component.Attacker,
			component.Damage,
			component.Inflictor,
			component.WorldPosition,
			Flags: component.DamageFlags
		);

		if ( Health.IsValid() )
		{
			Health.Health -= dmg.Damage;
		}
	}
}
