namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	//
	// Player
	//

	public virtual float ReachDistance { get; set; } = 150f;
	public virtual bool ShowNamePlate { get; set; } = true;
	public virtual bool ShowJobOnNamePlate { get; set; } = true;
	public virtual float SlowWalkSpeed { get; set; } = 75f;
	public const float WalkSpeed = 150f;
	public const float RunSpeed = 250f;
	public const float DuckedSpeed = 70f;
	public const float JumpSpeed = 300f;

	// RP Name
	public virtual bool RpNameEnabled { get; set; } = true;
	public virtual int RpNameMaxLength { get; set; } = 24;

	public virtual float PlayerInteractDistance { get; set; } = 100f;

	// Streak
	public virtual bool StreakMessageEnabled { get; set; } = true;

	// AFK System
	public virtual bool AfkEnabled { get; set; } = true;
	public virtual bool ShowAfkOnNamePlate { get; set; } = true;
	public virtual bool AfkDemoteEnabled { get; set; } = true;
	public virtual float AfkCheckInterval { get; set; } = 2.5f;
	public virtual float TimeUntilAfk { get; set; } = 300f; // 5 minutes
	public virtual float TimeUntilAfkDemote { get; set; } = 3600f; // 60 minutes
	public virtual float AfkMovementThreshold { get; set; } = 0.5f; // Minimum movement to not be AFK

	// Armor
	public virtual float MaxArmor { get; set; } = 100f;
	public virtual float BaseArmorReduction { get; set; } = 0.775f;
	public virtual float BaseHelmetReduction { get; set; } = 0.775f;

	// Fall Damage
	public virtual bool EnableFallDamage { get; set; } = true;
	public virtual float FallDamageMinimumDistance { get; set; } = 250f; // Falling over this distance is considered a damaging fall
	public virtual float FallDamageDeathDistance { get; set; } = 500f; // If you fall this distance you die
	public virtual float FallDamageMultiplier { get; set; } = 1.7f; // Multiply damage amount by this much

	// Medkit Revive
	public virtual float ReviveHealthAmount { get; set; } = 5.0f;
	public virtual int ReviveRequiredHits { get; set; } = 5;
	public virtual float ReviveTimeLimit { get; set; } = 8f;
	public virtual float ReviveDistanceBuffer { get; set; } = 25f;
	public virtual float ReviveTolerance { get; set; } = 4f;
	public virtual int ReviveZoneMinWidth { get; set; } = 10;
	public virtual int ReviveZoneMaxWidth { get; set; } = 14;
	public virtual float ReviveZoneMinDistance { get; set; } = 20f;
	public virtual int ReviveZoneMinStartPosition { get; set; } = 15;
	public virtual int ReviveZoneMaxStartPosition { get; set; } = 70;
	public virtual float ReviveIndicatorSpeed { get; set; } = 90f;
	public virtual float ReviveIndicatorSmoothingFactor { get; set; } = 0.1f;
}

