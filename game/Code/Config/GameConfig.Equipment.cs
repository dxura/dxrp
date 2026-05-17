namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	//
	// Tools
	//

	public virtual bool DupeWorkshopEnabled { get; set; } = true;
	public virtual string DupeWorkshopType { get; set; } = "dxdupe";

	//
	// Breach System
	//
	public virtual float PryMaxDistance { get; set; } = 100f;
	public virtual float PryDuration { get; set; } = 12.0f;
	public virtual bool FadingDoorRepairEnabled { get; set; } = true;
	public virtual bool FadingDoorHealthEnabled { get; set; } = true;
	public virtual bool DoorRepairEnabled { get; set; } = true;
	public virtual float RepairDuration { get; set; } = 30f;
	public virtual float BreachDuration { get; set; } = 180f; // 3 minutes

	//
	// Equipment
	//

	// Spread
	public virtual float BaseSpreadAmount { get; set; } = 0.05f;
	public virtual float SpreadVelocityLimit { get; set; } = 350f;
	public virtual float VelocitySpreadScale { get; set; } = 0.1f;
	public virtual float CrouchSpreadScale { get; set; } = 0.5f;
	public virtual float AirSpreadScale { get; set; } = 2.0f;
	public virtual float AimSpread { get; set; } = 0f;
	public virtual float AimVelocitySpreadScale { get; set; } = 0.5f;

	// Medkit Equipment
	public virtual bool MedkitHealOnlyAllowHealingPlayers { get; set; } = true;
	public virtual float MedkitHealAmount { get; set; } = 10f;
	public virtual float MedKitSelfHealReductionPercent { get; set; } = 0.35f;
	public virtual float MedkitOverHealPercent { get; set; } = 1.25f;
}

