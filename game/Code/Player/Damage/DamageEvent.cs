namespace Dxura.RP.Game;

[Flags]
public enum HitboxTags
{
	None = 0,
	Head = 1,
	Chest = 2,
	Stomach = 4,
	Clavicle = 8,
	Arm = 16,
	Hand = 32,
	Leg = 64,
	Ankle = 128,
	Spine = 256,
	Neck = 512,

	UpperBody = Neck | Chest | Clavicle,
	LowerBody = Stomach
}

[Flags]
public enum DamageFlags
{
	None = 0,

	/// <summary>
	///     The victim was wearing kevlar.
	/// </summary>
	Armor = 1,

	/// <summary>
	///     The victim was wearing a helmet.
	/// </summary>
	Helmet = 2,

	/// <summary>
	///     This was a knife attack.
	/// </summary>
	Melee = 4,

	/// <summary>
	///     This was some kind of explosion.
	/// </summary>
	Explosion = 8,

	/// <summary>
	///     The victim fell.
	/// </summary>
	FallDamage = 16,

	/// <summary>
	///     The victim was burned.
	/// </summary>
	Burn = 32,

	/// <summary>
	///     Did the attacker shoot through a wall?
	/// </summary>
	WallBang = 64,

	/// <summary>
	///     Was the attacker in the air when doing this damage?
	/// </summary>
	AirShot = 128
}

/// <summary>
///     Information about damage being dealt to a component.
/// </summary>
public record DamageInfo(
	Component Attacker,
	float Damage,
	Component? Inflictor = null,
	Vector3 Position = default,
	Vector3 Force = default,
	HitboxTags Hitbox = default,
	DamageFlags Flags = DamageFlags.None,
	float ArmorDamage = 0f,
	bool RemoveHelmet = false )
{
	// ReSharper disable once UnusedMember.Global
	public DamageInfo() : this( null!, 0f ) {}

	/// <summary>
	///     Who took damage?
	/// </summary>
	public Component Victim { get; init; } = null!;

	/// <inheritdoc cref="DamageFlags.Armor" />
	public bool HasArmor => Flags.HasFlag( DamageFlags.Armor );

	/// <inheritdoc cref="DamageFlags.Helmet" />
	public bool HasHelmet => Flags.HasFlag( DamageFlags.Helmet );

	/// <inheritdoc cref="DamageFlags.Melee" />
	public bool WasMelee => Flags.HasFlag( DamageFlags.Melee );

	/// <inheritdoc cref="DamageFlags.Explosion" />
	public bool WasExplosion => Flags.HasFlag( DamageFlags.Explosion );

	/// <inheritdoc cref="DamageFlags.FallDamage" />
	public bool WasFallDamage => Flags.HasFlag( DamageFlags.FallDamage );

	/// <summary>
	///     How long since this damage info event happened?
	/// </summary>
	public RealTimeSince TimeSinceEvent { get; init; } = 0;

	public override string ToString()
	{
		return $"\"{Attacker}\" - \"{Victim}\" with \"{Inflictor}\" ({Damage} damage)";
	}
}

/// <summary>
///     Interface for handling damage-related events in the scene.
/// </summary>
public interface IDamageEvents : ISceneEvent<IDamageEvents>
{
	/// <summary>
	///     Called when a component is about to take damage, allowing modification of the damage info.
	/// </summary>
	void OnModifyDamageTaken( Component victim, ref DamageInfo damage ) {}

	/// <summary>
	///     Called when a component is about to give damage, allowing modification of the damage info.
	/// </summary>
	void OnModifyDamageGiven( Component attacker, ref DamageInfo damage ) {}

	/// <summary>
	///     Called after a component has taken damage.
	/// </summary>
	void OnDamageTakenHost( Component victim, DamageInfo damage ) {}

	/// <summary>
	///     Called after a component has given damage.
	/// </summary>
	void OnDamageGivenHost( Component attacker, DamageInfo damage ) {}

	/// <summary>
	///     Called when damage results in a kill.
	/// </summary>
	void OnKillHost( Component victim, DamageInfo damage ) {}
}

/// <summary>
///     Extension methods for modifying damage information.
/// </summary>
public static class DamageExtensions
{
	/// <summary>
	///     Clears all health and armor damage from this event.
	/// </summary>
	public static void ClearDamage( ref DamageInfo info )
	{
		info = info with
		{
			Damage = 0f, ArmorDamage = 0f, RemoveHelmet = false
		};
	}

	/// <summary>
	///     Scales health damage by the given multiplier.
	/// </summary>
	public static void ScaleDamage( ref DamageInfo info, float scale )
	{
		info = info with
		{
			Damage = info.Damage * scale
		};
	}

	/// <summary>
	///     Flag that the victim's helmet should be removed when this damage is applied.
	/// </summary>
	public static void RemoveHelmet( ref DamageInfo info )
	{
		info = info with
		{
			RemoveHelmet = true
		};
	}

	/// <summary>
	///     Scales damage by damageScale, adding the difference to armor damage.
	/// </summary>
	public static void ApplyArmor( ref DamageInfo info, float damageScale )
	{
		var reduced = info.Damage * damageScale;
		info = info with
		{
			Damage = reduced, ArmorDamage = info.Damage - reduced
		};
	}

	/// <summary>
	///     Adds a flag to this damage event.
	/// </summary>
	public static void AddFlag( ref DamageInfo info, DamageFlags flag )
	{
		info = info with
		{
			Flags = info.Flags | flag
		};
	}

	/// <summary>
	///     Removes a flag from this damage event.
	/// </summary>
	public static void WithoutFlag( ref DamageInfo info, DamageFlags flag )
	{
		info = info with
		{
			Flags = info.Flags & ~flag
		};
	}
}

public static class SceneTraceExtensions
{
	public static HitboxTags GetHitboxTags( this SceneTraceResult tr )
	{
		if ( tr.Hitbox is null )
		{
			return HitboxTags.None;
		}

		var tags = HitboxTags.None;

		foreach ( var tag in tr.Hitbox.Tags )
		{
			if ( Enum.TryParse<HitboxTags>( tag, true, out var hitboxTag ) )
			{
				tags |= hitboxTag;
			}
		}

		return tags;
	}
}

public class HitboxConfig
{
	public HitboxTags Tags { get; set; }
	public float DamageScale { get; set; } = 1f;
	public bool ArmorProtects { get; set; }
	public bool HelmetProtects { get; set; }
}
