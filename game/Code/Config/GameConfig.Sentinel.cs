namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	//
	// Sentinel (Anticheat)
	//
	public virtual bool SentinelEnabled { get; set; } = true;
	public virtual bool SentinelReportingEnabled { get; set; } = true;

	// Sentinel: Flight
	public virtual bool SentinelFlightEnabled { get; set; } = true;
	public virtual bool SentinelFlightReportingEnabled { get; set; } = true;
	public virtual bool SentinelFlightPunishmentEnabled { get; set; } = true;
	public virtual float SentinelMaxFlightTime { get; set; } = 10f;
	public virtual float SentinelFlightMinDistance { get; set; } = 150f;

	// Sentinel: Noclip
	public virtual bool SentinelNoclipEnabled { get; set; } = true;
	public virtual bool SentinelNoclipReportingEnabled { get; set; } = true;
	public virtual bool SentinelNoclipPunishmentEnabled { get; set; } = true;

	// Sentinel: Bound
	public virtual bool SentinelBoundEnabled { get; set; } = true;
	public virtual bool SentinelBoundReportingEnabled { get; set; } = true;
	public virtual bool SentinelBoundPunishmentEnabled { get; set; } = true;

	// Sentinel: Scale
	public virtual bool SentinelScaleEnabled { get; set; } = true;
	public virtual bool SentinelScaleReportingEnabled { get; set; } = true;
	public virtual bool SentinelScalePunishmentEnabled { get; set; } = true;

	// Sentinel: Listener
	public virtual bool SentinelListenerEnabled { get; set; } = true;
	public virtual bool SentinelListenerReportingEnabled { get; set; } = true;
	public virtual bool SentinelListenerPunishmentEnabled { get; set; } = true;

	// Sentinel: Teleport
	public virtual bool SentinelTeleportEnabled { get; set; } = true;
	public virtual bool SentinelTeleportReportingEnabled { get; set; } = false;
	public virtual bool SentinelTeleportPunishmentEnabled { get; set; } = true;
	public virtual float SentinelTeleportThreshold { get; set; } = 250f; // Extra flat distance buffer added after speed allowance

	// Sentinel: Netspam
	public virtual bool SentinelNetSpamEnabled { get; set; } = true;
	public virtual bool SentinelNetSpamReportingEnabled { get; set; } = true;
	public virtual float SentinelNetSpamCheckInterval { get; set; } = 5f; // How often to check (in seconds)
	public virtual int SentinelNetSpamViolationCountThreshold { get; set; } = 4;
	public virtual float SentinelNetSpamBandwidthThreshold { get; set; } = 1_000_000f; // Absolute minimum bandwidth in bytes/sec (~977 KB/s, about 1 MB/s)
	public virtual float SentinelNetSpamBurstThresholdMultiplier { get; set; } = 3f; // How far above the threshold a sample must be before it counts immediately
	public virtual int SentinelNetSpamConsecutiveSampleThreshold { get; set; } = 4; // How many back-to-back elevated samples are required
	public virtual float SentinelNetSpamGracePeriod { get; set; } = 120f; // Ignore heavy join/sync bursts shortly after connecting
	public virtual float SentinelNetSpamViolationDecayTime { get; set; } = 60f; // 1 minute cooldown after violation

	// Sentinel: Mass Kill
	public virtual bool SentinelMassKillEnabled { get; set; } = true;
	public virtual bool SentinelMassKillReportingEnabled { get; set; } = true;
	public virtual bool SentinelMassKillPunishmentEnabled { get; set; } = true;
	public virtual int SentinelMassKillThreshold { get; set; } = 10; // kills within the window
	public virtual float SentinelMassKillWindow { get; set; } = 5f; // seconds
}
