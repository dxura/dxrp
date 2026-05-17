using Dxura.RP.Shared;
namespace Dxura.RP.Game.Sentinel;

public partial class Sentinel : GameObjectSystem<Sentinel>
{
	public bool Enabled { get; set; } = true;

	[ConCmd( "dx_toggle_sentinel" )] public static void ToggleSentinel()
	{
		if ( !RankSystem.HasLocalPermission( Permission.DebugAccess ) )
		{
			return;
		}

		Current?.ToggleSentinelHost();
	}

	public Sentinel( Scene scene ) : base( scene )
	{
		if ( Scene.IsEditor )
		{
			return;
		}

		Listen( Stage.SceneLoaded, 100, Loaded, "Sentinel Start" );
		Listen( Stage.FinishUpdate, 100, Process, "Sentinel Loop" );
	}

	private void Loaded()
	{
		Enabled = Config.Current.Game.SentinelEnabled;
	}

	private void Process()
	{
		if ( !Networking.IsHost || !Enabled || !Networking.IsActive )
		{
			return;
		}

		ProcessFlight();
		ProcessTeleport();
		ProcessBound();
		ProcessScale();
		ProcessNet();
		ProcessListener();
		ProcessMassKill();
	}

	private bool IsExempt( Player player )
	{
		if ( !player.IsValid() )
		{
			return true;
		}

		// Exempt very new connections
		var connectionAge = DateTimeOffset.UtcNow - player.Connection?.ConnectionTime;
		if ( player.Connection?.IsConnecting == true || connectionAge < TimeSpan.FromSeconds( 10 ) )
		{
			return true;
		}

		if ( player.Network.Owner == null )
		{
			return true;
		}

		// Check bypass things
		if ( RankSystem.HasPermission( player.SteamId, Permission.Noclip ))
		{
			return true;
		}

		return false;
	}

	internal static void ReportViolation( Player player, string exploit, string detail = "" )
	{
		Current?.RecordViolation( player, exploit, detail );
	}

	private static bool CanReportViolations => Config.Current.Game.SentinelReportingEnabled;

	private void RecordViolation( Player player, string exploit, string detail = "", bool typeReportingEnabled = true )
	{
		if ( !CanReportViolations || !typeReportingEnabled )
		{
			return;
		}

		var log = $"[Sentinel] Player {player.DisplayName} ({player.SteamId}) violation: {exploit} {detail}";
		Log.Info( log );

		// Log to server (throttled to once every 5 seconds per player)
		if ( !Cooldown.Current.CheckAndStartCooldown( $"{player}:sentinel", 5 ) )
		{
			_ = ServerApiClient.SanctionPlayer( player.SteamId, new CreateSanctionDto
			{
				Reason = exploit, Notes = $"Sentinel violation: {detail}", Type = SanctionType.Automatic
			} );
		}

		// Capture screenshot (throttled to once every minute)
		if ( !Cooldown.Current.CheckAndStartCooldown( $"{player}:sentinel:screenshot", 60f ) )
		{
			AdminSystem.Instance.ForceScreenshotHost( player.SteamId );
		}
	}

	[Rpc.Host]
	private void ToggleSentinelHost()
	{
		var caller = Rpc.Caller;
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.DebugFull ) )
		{
			return;
		}

		Enabled = !Enabled;
		var status = Enabled ? "enabled" : "disabled";

		caller.SendLog( LogLevel.Info, $"Sentinel has been {status}." );

		Log.Info( $"Sentinel has been {status} by {Player.Local.DisplayName}." );
	}
}
