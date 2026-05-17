namespace Dxura.RP.Game;

// The sandbox experience inspired by Garry's Mod
public class SandboxGameConfig : GameConfig
{
	//
	// General
	//
	public override string Identifier => "dxura.sandbox";

	public override bool IsLobbySupported { get; set; } = true;

	public override int RespawnTime { get; set; } = 5;
	public override bool NoClip { get; set; } = true;

	public override bool SentinelEnabled { get; set; } = false;
	public override bool GraceReconnectEnabled { get; set; } = true;
	public override bool AutoUpdateEnabled { get; set; } = false;

	// Branding
	public override string DashboardName { get; set; } = "DXplore";
	public override string DashboardDescription { get; set; } = "Sandbox for s&box";

	//
	// RP Systems (disabled in sandbox)
	//
	public override bool JobsEnabled { get; set; } = false;
	public override bool MoneyEnabled { get; set; } = false;
	public override bool DemoteEnabled { get; set; } = false;
	public override bool SalaryPaymentEnabled { get; set; } = false;
	public override bool VotingEnabled { get; set; } = false;
	public override bool GlassRepairEnabled { get; set; } = false;
	public override bool GarbageEnabled { get; set; } = false;
	public override bool CustomJobEnabled { get; set; } = false;
	public override bool SnapshotEnabled { get; set; } = false;

	// Governance (disabled in sandbox)
	public override bool GovernanceLawEnabled { get; set; } = false;
	public override bool GovernanceWantedEnabled { get; set; } = false;
	public override bool GovernanceLockdownEnabled { get; set; } = false;
	public override bool GovernanceJailEnabled { get; set; } = false;
	public override bool GovernanceMayorAnnounceEnabled { get; set; } = false;
	public override float GovernanceAnnouncementDuration { get; set; } = 0f;

	// Minigames
	public override bool MinigamesAutoEnabled { get; set; } = true;

	// Props
	public override string? RestrictCloudOrg { get; set; } = null;
	public override bool PreventPropExploits { get; set; } = false;
	public override int? MaxPropSize { get; set; } = null;

	// Player
	public override bool ShowJobOnNamePlate { get; set; } = false;
	public override bool StreakMessageEnabled { get; set; } = false;
	public override bool AfkDemoteEnabled { get; set; } = false;

	// Pocket
	public override bool PocketEnabled { get; set; } = true;

	// Chat
	public override bool OocChatEnabled { get; set; } = false;
	public override int ChatMaxLength { get; set; } = 300;
	public override int ChatMaxDistance { get; set; } = 10000000;

	// Text Moderation
	public override bool ModerateText { get; set; } = false;
	public override string[] TextWordBlacklist { get; set; } = [];

	//
	// UI
	//
	public override bool ShowJobPlayerInfoHud { get; set; } = false;
	public override bool ShowTimeDisplayHud { get; set; } = false;
	public override bool ShowJobsMenu { get; set; } = false;
	public override bool ShowPlayerInfoMenu { get; set; } = false;
	public override bool ShowPlayersRpColumnsMenu { get; set; } = false;

	// TabMenu
	public override bool ShowDashboardMenu { get; set; } = true;
	public override bool ShowMarketMenu { get; set; } = true;

	//
	// Limits
	//
	public override uint PropLimit { get; set; } = 1000;
	public override uint TextLimit { get; set; } = 25;
	public override uint FrameLimit { get; set; } = 30;

	// Wire
	public override uint GateWireLimit { get; set; } = 25;
	public override uint ConstantWireLimit { get; set; } = 15;
	public override uint ButtonWireLimit { get; set; } = 25;
	public override uint KeypadWireLimit { get; set; } = 25;
	public override uint DelayWireLimit { get; set; } = 15;
	public override uint ForcerWireLimit { get; set; } = 15;
	public override uint IntervalWireLimit { get; set; } = 10;
	public override uint LedWireLimit { get; set; } = 25;
	public override uint MemoryWireLimit { get; set; } = 25;
	public override uint MoneyPotWireLimit { get; set; } = 15;
	public override uint NotiferWireLimit { get; set; } = 10;
	public override uint ScreenWireLimit { get; set; } = 15;
	public override uint SpeakerWireLimit { get; set; } = 15;
	public override uint TriggerWireLimit { get; set; } = 15;
	public override uint UserWireLimit { get; set; } = 15;
	public override uint SynthesizerWireLimit { get; set; } = 15;

	//
	// Cooldowns
	//
	public override float ChatCooldown { get; set; } = 0.5f;
	public override float ConstructCooldown { get; set; } = 0.5f;
	public override float SuicideCooldown { get; set; } = 15f;
	public override float StackerCooldown { get; set; } = 1f;
	public override float DupeCooldown { get; set; } = 1f;
	public override float TvCooldown { get; set; } = 1f;
	public override float NoCollideCooldown { get; set; } = 1f;
	public override float EntityCooldown { get; set; } = 0.5f;
}
