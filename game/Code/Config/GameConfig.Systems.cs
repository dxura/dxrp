namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	//
	// Systems
	//

	// Salary Payment System
	public virtual bool SalaryPaymentEnabled { get; set; } = true;

	// Voting System
	public virtual bool VotingEnabled { get; set; } = true;
	public virtual bool DemoteEnabled { get; set; } = true;
	public virtual int DemoteMinPlaytime { get; set; } = 120; // minutes

	// Glass Repair System
	public virtual bool GlassRepairEnabled { get; set; } = true;

	// Pocket System
	public virtual bool PocketEnabled { get; set; } = true;
	public virtual bool DropPocketsOnDeath { get; set; } = true;
	public virtual bool DropPocketsOnJobChange { get; set; } = true;
	public virtual int MaxPocketItems { get; set; } = 6;

	// Faction System
	public virtual bool FactionsEnabled { get; set; } = false;
	public virtual uint FactionCreateCost { get; set; } = 500_000;

	// Drop System
	public virtual bool DropWeaponOnDeath { get; set; } = true;
	public virtual bool DropWeaponsOnJobChange { get; set; } = true;
	public virtual bool DropWalletOnJobChange { get; set; } = true;

	// Garbage System
	public virtual bool GarbageEnabled { get; set; } = true;
	public virtual int GarbageSpawnInterval { get; set; } = 20;
	public virtual uint MaxGarbageCount { get; set; } = 20;
	public virtual int GarbageDespawnTime { get; set; } = 7200; // 1 hour

	// Snapshot System
	public virtual bool SnapshotEnabled { get; set; } = true;
	public virtual float SnapshotInitialSaveGrace { get; set; } = 600f;
	public virtual float SnapshotSaveInterval { get; set; } = 300f;
	
	public virtual float WireTick { get; set; } = 0.3f; // How often wires are processed

	// Forcer
	public virtual string[] ForcerExcludeTags { get; set; } = ["money"];

	// Grace reconnect
	public virtual bool GraceReconnectEnabled { get; set; } = true;
	public virtual int GraceReconnectTime { get; set; } = 480; // 8 minutes

	// Other
	public virtual bool JobsEnabled { get; set; } = true;
	public virtual bool MoneyEnabled { get; set; } = true;
	public virtual bool CustomJobEnabled { get; set; } = true;
	public virtual int DroppedEquipmentDestroyTime { get; set; } = 7200; // 2 hours
	public virtual int DroppedInventoryItemDestroyTime { get; set; } = 7200; // 2 hours
	public virtual int DroppedMoneyDestroyTime { get; set; } = 1800; // 30 minutes
	public virtual bool JobEntitiesClearOnChange { get; set; } = true;

	// Coin Flipping
	public virtual bool CoinFlipEnabled { get; set; } = true;
	public virtual uint CoinFlipMinimalBet { get; set; } = 10000;
	public virtual int CoinFlipDuration { get; set; } = 30; // 30 seconds

	//
	// Occlusion
	//
	public virtual bool OcclusionEnabled { get; set; } = true;

	//
	// Minigames
	//
	public virtual bool MinigamesEnabled { get; set; } = true;
	public virtual bool MinigamesAutoEnabled { get; set; } = false;
	public virtual int MinigameAutoInterval { get; set; } = 2700; // 45 minutes
	public virtual int MinigameAutoMinPlayers { get; set; } = 2; // Minimum players to start an auto minigame (before even seeding, not afk)

	//
	// AutoUpdater
	//
	public virtual bool AutoUpdateEnabled { get; set; } = true;
	public virtual int AutoUpdateCheckInterval { get; set; } = 120; // Every 2 minutes

	// Events
	public virtual bool EventsEnabled { get; set; } = true;
	public virtual int MinTimeBetweenEvents { get; set; } = 2700; // 45 minutes
	public virtual int MaxTimeBetweenEvents { get; set; } = 3600; // 60 minutes

	// Printer
	public virtual bool PrinterDecayEnabled { get; set; } = true;
	public virtual float PrinterDestroyAfterDisconnectTime { get; set; } = 3600f;

	// Recycler
	public virtual int RecyclerProcessInterval { get; set; } = 10;
	public virtual string[] RecyclerAcceptedTags { get; set; } = [Constants.GarbageTag, Constants.RagdollTag, Constants.EntityTag];
	public virtual uint RecyclerMaxQueue { get; set; } = 8;
	public virtual float RecyclerEntityRefundPercent { get; set; } = 0.50f;
	public virtual uint RecyclerGarbageKnifeDropChance { get; set; } = 2;
	public virtual uint RecyclerGarbagePistolDropChance { get; set; } = 10;
	public virtual uint RecyclerGarbageMoneyDropChance { get; set; } = 88;
	public virtual int RecyclerGarbageMoneyMinAmount { get; set; } = 30;
	public virtual int RecyclerGarbageMoneyMaxAmount { get; set; } = 60;

	//
	// Statuses (Config)
	//

	// Satiated Status
	public virtual float SatiatedHealPerStack { get; set; } = 1f; // Health healed per second per stack
}

