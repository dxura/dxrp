using Dxura.RP.Game.UI;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

/// <summary>
///     A health component for any kind of GameObject.
/// </summary>
public class HealthComponent : Component
{
	/// <summary>
	///     Are we in god mode?
	/// </summary>
	[Property]
	[Sync( SyncFlags.FromHost )]
	public bool IsGodMode { get; set; }

	/// <summary>
	///     How long has it been since life state changed?
	/// </summary>
	public TimeSince TimeSinceLifeStateChanged { get; private set; } = 1f;

	/// <summary>
	///     What's our health?
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	[Property]
	[ReadOnly]
	public float Health { get; set; } = 100f;

	[Property] [Group( "Setup" )] public float MaxHealth { get; set; } = 100f;

	/// <summary>
	///     What's our life state?
	/// </summary>
	[Group( "Life State" )]
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnStatePropertyChanged ) )]
	public LifeState State { get; set; }

	protected void OnStatePropertyChanged( LifeState oldValue, LifeState newValue )
	{
		TimeSinceLifeStateChanged = 0f;
	}

	protected override void OnStart()
	{
		Health = MaxHealth;
	}

	public void TakeDamageHost( DamageInfo damageInfo )
	{
		Assert.True( Networking.IsHost );

		// Set this component as the victim and apply initial modifications
		damageInfo = WithThisAsVictim( damageInfo );

		// Let components modify the damage through the event system
		IDamageEvents.PostToGameObject( damageInfo.Attacker.GameObject,
			x => x.OnModifyDamageGiven( damageInfo.Attacker, ref damageInfo ) );
		IDamageEvents.PostToGameObject( GameObject, x => x.OnModifyDamageTaken( this, ref damageInfo ) );
		ApplyGlobalDamageModifications( ref damageInfo );

		if ( IsGodMode )
		{
			return;
		}

		// Apply damage
		if ( !(damageInfo.Damage > 0) )
		{
			return;
		}

		Health = Math.Max( 0f, Health - damageInfo.Damage );

		// Broadcast damage events
		IDamageEvents.PostToGameObject( GameObject, x => x.OnDamageTakenHost( this, damageInfo ) );
		IDamageEvents.PostToGameObject( damageInfo.Attacker.GameObject,
			x => x.OnDamageGivenHost( damageInfo.Attacker, damageInfo ) );

		// Handle death
		if ( Health <= 0f && State == LifeState.Alive )
		{
			Health = 0f;
			State = LifeState.Dead;

			// Broadcast kill event
			IDamageEvents.PostToGameObject( GameObject, x => x.OnKillHost( this, damageInfo ) );

			// Record kill in feed
			KillFeed.RecordEventHost( damageInfo );
		}
	}

	private static readonly List<HitboxConfig> DefaultHitboxConfigs = new()
	{
		new() { Tags = HitboxTags.Head, DamageScale = 1f, HelmetProtects = true },
		new() { Tags = HitboxTags.UpperBody | HitboxTags.Arm, ArmorProtects = true },
		new() { Tags = HitboxTags.LowerBody, DamageScale = 1.25f },
		new() { Tags = HitboxTags.Leg, DamageScale = 0.75f }
	};

	private static void ApplyGlobalDamageModifications( ref DamageInfo damageInfo )
	{
		var config = Config.Current.Game;

		if ( damageInfo.WasFallDamage && !config.EnableFallDamage )
		{
			DamageExtensions.ClearDamage( ref damageInfo );
			return;
		}

		var inflictor = damageInfo.Inflictor as Equipment;
		var armorReduction = inflictor?.ArmorReduction ?? config.BaseArmorReduction;
		var helmetReduction = inflictor?.HelmetReduction ?? config.BaseHelmetReduction;

		var damageScale = 1f;
		var armorScale = 1f;
		var removeHelmet = false;

		var hitboxTags = damageInfo.Hitbox;
		if ( DefaultHitboxConfigs.FirstOrDefault( x => (x.Tags & hitboxTags) != 0 ) is {} hitbox )
		{
			damageScale = hitbox.DamageScale;

			if ( hitbox.HelmetProtects && (damageInfo.Flags & DamageFlags.Helmet) != 0 )
			{
				armorScale = helmetReduction;
				removeHelmet = true;
			}
			else if ( hitbox.ArmorProtects && (damageInfo.Flags & DamageFlags.Armor) != 0 )
			{
				armorScale = armorReduction;
			}
		}

		DamageExtensions.ScaleDamage( ref damageInfo, damageScale );
		DamageExtensions.ApplyArmor( ref damageInfo, armorScale );

		if ( removeHelmet )
		{
			DamageExtensions.RemoveHelmet( ref damageInfo );
		}
	}

	private DamageInfo WithThisAsVictim( DamageInfo damageInfo )
	{
		var extraFlags = DamageFlags.None;
		var hitbox = damageInfo.Hitbox;

		if ( damageInfo.WasExplosion || damageInfo.WasMelee )
		{
			hitbox = HitboxTags.UpperBody;
		}

		if ( damageInfo.WasFallDamage )
		{
			hitbox = HitboxTags.Leg;
		}

		return damageInfo with
		{
			Victim = this, Hitbox = hitbox, Flags = damageInfo.Flags | extraFlags
		};
	}
}

/// <summary>
///     The component's life state.
/// </summary>
public enum LifeState
{
	Alive,
	Dead
}
