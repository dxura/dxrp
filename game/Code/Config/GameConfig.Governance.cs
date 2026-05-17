namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	//
	// Governance (RP)
	//

	// Features
	public virtual bool GovernanceLockdownEnabled { get; set; } = true;
	public virtual bool GovernanceJailEnabled { get; set; } = true;
	public virtual bool GovernanceLawEnabled { get; set; } = true;
	public virtual bool GovernanceMayorAnnounceEnabled { get; set; } = true;
	public virtual bool GovernanceWantedEnabled { get; set; } = true;


	// Laws
	public virtual int MaxLaws { get; set; } = 10;
	public virtual string[] DefaultLaws { get; set; } =
	[
		"#governance.laws.default.1",
		"#governance.laws.default.2",
		"#governance.laws.default.3"
	];

	// Mayor Announce
	public virtual int MayorAnnounceMaxLength { get; set; } = 250; // 5 minutes
	public virtual float GovernanceAnnouncementDuration { get; set; } = 5f;

	// Lockdown
	public virtual int LockdownDuration { get; set; } = 300; // 5 minutes
	public virtual bool LockdownDoAlarm { get; set; } = true;
	public virtual bool LockdownDoAnnouncements { get; set; } = true;

	// Wanted
	public virtual int WantedTime { get; set; } = 300;

	// Jail
	public virtual int JailTime { get; set; } = 120;

	// Warrant
	public virtual int WarrantTime { get; set; } = 300;

	// Tax System
	public virtual bool GovernanceTaxEnabled { get; set; } = true;
	public virtual float TaxRateDefault { get; set; } = 0.05f;
	public virtual float TaxRateMax { get; set; } = 0.20f;
	public virtual bool TaxExemptGovernment { get; set; } = false;
	public virtual uint TaxExemptBankBalanceThreshold { get; set; } = 20000;
	public virtual int TaxExemptPlayTimeThreshold { get; set; } = 120;
	public virtual float TaxRateCooldown { get; set; } = 60f;
	public virtual int TownNameMaxLength { get; set; } = 16;
	public virtual uint TownNameCost { get; set; } = 5000;
	public virtual bool TaxResetTreasuryOnMayorDeath { get; set; } = true;
	public virtual bool TaxResetTreasuryOnMayorElect { get; set; } = false;

	// PD Upgrades
	public virtual uint PdUpgradeOverhealCost { get; set; } = 8000;
	public virtual uint PdUpgradeMp5Cost { get; set; } = 16000;
	public virtual uint PdUpgradeShotgunCost { get; set; } = 24000;
	public virtual uint PdUpgradeM4Cost { get; set; } = 32000;
	public virtual uint PdUpgradeAmmoCacheCost { get; set; } = 1600;
	public virtual uint PdUpgradeRecruitmentDriveCost { get; set; } = 4000;
	public virtual float PdUpgradeDuration { get; set; } = 2700f;
	public virtual float PdUpgradeAmmoCacheDuration { get; set; } = 5400f;
	public virtual float PdUpgradeDecayInterval { get; set; } = 600f;

	// Bank Raid
	public virtual float BankRaidVanishPercent { get; set; } = 0.125f;

	// Hitman System
	public virtual bool HitmanEnabled { get; set; } = true;
	public virtual int MaxHitReasonLength { get; set; } = 48;
	public virtual uint HitPriceIncrement { get; set; } = 500;
	public virtual uint MinHitPrice { get; set; } = 1000;
	public virtual uint MaxHitPrice { get; set; } = 50000;
	public virtual float HitmanRequestTimeout { get; set; } = 60f;
	public virtual float HitmanActiveHitDuration { get; set; } = 1200f; // 20 minutes
}

