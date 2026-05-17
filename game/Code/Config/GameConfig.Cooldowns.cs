namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	//
	// Cooldowns
	//

	// General
	public virtual float PrivateMessageCooldown { get; set; } = 1;
	public virtual float AdvertCooldown { get; set; } = 60;
	public virtual float MeCooldown { get; set; } = 1;
	public virtual float StaffCooldown { get; set; } = 1;
	public virtual float CommandCooldown { get; set; } = 1;
	public virtual float ActionCooldown { get; set; } = 1.5f;
	public virtual float ActionQuickCooldown { get; set; } = 0.5f;
	public virtual float ActionLongCooldown { get; set; } = 120f;
	public virtual float SoundCooldown { get; set; } = 0.25f;
	public virtual float EntityCooldown { get; set; } = 1;
	public virtual float ConstructCooldown { get; set; } = 1f;
	public virtual float ConstructUpdateCooldown { get; set; } = 1f;
	public virtual float EquipmentDropCooldown { get; set; } = 1;
	public virtual float SuicideCooldown { get; set; } = 300;
	public virtual float PanicCooldown { get; set; } = 300;
	public virtual float FallDamageCooldown { get; set; } = 0.75f;
	public virtual float PocketCooldown { get; set; } = 0.5f;
	public virtual float UndoCooldown { get; set; } = 0.25f;

	// Chat
	public virtual float ChatCooldown { get; set; } = 0.75f;
	public virtual int ChatSoundNotifyCooldown { get; set; } = 30;

	// Tools
	public virtual float ToolEffectsCooldown { get; set; } = 1;
	public virtual float StackerCooldown { get; set; } = 3;
	public virtual float DupeCooldown { get; set; } = 30;
	public virtual int PlayerSitCooldown { get; set; } = 1;
	public virtual float FadingDoorCooldown { get; set; } = 1.5f;
	public virtual float ScaleCooldown { get; set; } = 1f;

	// Wire
	public virtual float WireUserCooldown { get; set; } = 1f;
	public virtual float WireSynthesizeCooldown { get; set; } = 1f;
	public virtual float WireScreenCameraRaycastInterval { get; set; } = 1.5f;

	// Equipment
	public virtual float HealCooldown { get; set; } = 0.5f;
	public virtual float ReviveCooldown { get; set; } = 5f;

	public virtual float TvCooldown { get; set; } = 5;
	public virtual float NoCollideCooldown { get; set; } = 5;
	public virtual float RemoverCooldown { get; set; } = 0.5f;
	public virtual float FadingDoorCreateCooldown { get; set; } = 5f;

	public virtual float EquipmentHandCuffUseCooldown { get; set; } = 1;
	public virtual float HoboTauntCooldown { get; set; } = 10f;

	// Weapons
	public virtual float RefillAmmoCooldown { get; set; } = 5;
	public virtual float SwingCooldown { get; set; } = 0.5f;
	public virtual float ShootDryEffectsCooldown { get; set; } = 0.15f;
	public virtual float ReloadStartCooldown { get; set; } = 1f;
	public virtual float ReloadCancelCooldown { get; set; } = 1f;
	public virtual float ReloadEndCooldown { get; set; } = 1f;
	public virtual float DamageCooldown { get; set; } = 0.05f;

	// RP
	public virtual float VoteCooldown { get; set; } = 120;
	public virtual float JobVoteCooldown { get; set; } = 600;
	public virtual float ChangeCustomJobCooldown { get; set; } = 30;

	public virtual float MoneyCooldown { get; set; } = 2;
	public virtual int DemoteJobCooldown { get; set; } = 600;
	public virtual int DemoteJobReapplyCooldown { get; set; } = 600;
	public virtual float ArrestCooldown { get; set; } = 15;
	public virtual float UnArrestCooldown { get; set; } = 5;
	public virtual float JobChangeCooldown { get; set; } = 30;
	public virtual float LawChange { get; set; } = 3;
	public virtual float WantedCooldown { get; set; } = 5;
	public virtual float WarrantCooldown { get; set; } = 5;
	public virtual float PlayerWantedCooldown { get; set; } = 60;
	public virtual float PlayerWarrantCooldown { get; set; } = 900;
	public virtual int LockdownCooldown { get; set; } = 1800; // 30 minutes
	public virtual float MayorAnnounceCooldown { get; set; } = 300f; // 5 minutes

	// Utilities
	public virtual float UtilityClearCooldown { get; set; } = 30f;

	// Entities
	public virtual float DiceCooldown { get; set; } = 2.5f;
	public virtual float ShipmentUseCooldown { get; set; } = 1.5f;
	public virtual float PlanterHarvestCooldown { get; set; } = 1f;

	// World
	public virtual float GlassShatterCooldown { get; set; } = 1f;
	public virtual float DoorBuySellCooldown { get; set; } = 3;
	public virtual float SellAllDoorsCooldown { get; set; } = 60;
	public virtual float DoorUseCooldown { get; set; } = 0.5f;
	public virtual float DoorLockCooldown { get; set; } = 0.5f;
	public virtual float DoorKnockCooldown { get; set; } = 0.35f;
	public virtual float DoorBreachUseCooldown { get; set; } = 180;

	// Coin Flipping
	public virtual uint CoinFlipCooldown { get; set; } = 60;

	// Breach
	public virtual float PryCooldown { get; set; } = 3;
	public virtual float PryEffectsCooldown { get; set; } = 1;

	// Hitman
	public virtual float HitmanRequestCooldown { get; set; } = 60f;
	public virtual float HitPlayerCooldown { get; set; } = 600f; // 10 minutes

	// Misc
	public virtual float SharePhotoCooldown { get; set; } = 30;
}

