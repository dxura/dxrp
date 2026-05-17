using Dxura.RP.Shared;
namespace Dxura.RP.Game.Sentinel;

public partial class Sentinel
{
	private readonly Dictionary<long, Queue<RealTimeSince>> _massKillTimestamps = new();
	private TimeSince _lastMassKillCleanup = 0;

	internal static void NotifyKill( Player killer, Player victim )
	{
		if ( Current is not {} sentinel ) return;
		if ( !Networking.IsHost || !sentinel.Enabled ) return;
		sentinel.TrackKill( killer, victim );
	}

	private void TrackKill( Player killer, Player victim )
	{
		if ( !Config.Current.Game.SentinelMassKillEnabled ) return;
		if ( IsExempt( killer ) ) return;
		if ( !killer.IsValid() || killer == victim ) return;

		var window = Config.Current.Game.SentinelMassKillWindow;
		var threshold = Config.Current.Game.SentinelMassKillThreshold;

		if ( !_massKillTimestamps.TryGetValue( killer.SteamId, out var queue ) )
		{
			queue = new Queue<RealTimeSince>();
			_massKillTimestamps[killer.SteamId] = queue;
		}

		while ( queue.Count > 0 && (float)queue.Peek() > window )
			queue.Dequeue();

		queue.Enqueue( new RealTimeSince() );

		if ( queue.Count >= threshold )
		{
			DisciplineMassKill( killer, queue.Count, window );
			queue.Clear();
		}
	}

	private void ProcessMassKill()
	{
		if ( _massKillTimestamps.Count == 0 )
		{
			return;
		}

		if ( !Config.Current.Game.SentinelMassKillEnabled )
		{
			_massKillTimestamps.Clear();
			return;
		}

		if ( _lastMassKillCleanup < 30f )
		{
			return;
		}

		_lastMassKillCleanup = 0;
		CleanupMassKillTrackers();
	}

	private void CleanupMassKillTrackers()
	{
		var activePlayers = GameNetworkManager.Instance.Players.Keys.ToHashSet();
		var window = Config.Current.Game.SentinelMassKillWindow;
		var staleKeys = new List<long>();

		foreach ( var (steamId, queue) in _massKillTimestamps )
		{
			while ( queue.Count > 0 && (float)queue.Peek() > window )
			{
				queue.Dequeue();
			}

			if ( queue.Count == 0 || !activePlayers.Contains( steamId ) )
			{
				staleKeys.Add( steamId );
			}
		}

		foreach ( var steamId in staleKeys )
		{
			_massKillTimestamps.Remove( steamId );
		}
	}

	private void DisciplineMassKill( Player player, int killCount, float window )
	{
		var config = Config.Current.Game;

		RecordViolation(
			player,
			"Mass Kill",
			$"{killCount} kills within {window:0.0}s",
			config.SentinelMassKillReportingEnabled
		);

		if ( !config.SentinelMassKillPunishmentEnabled )
		{
			return;
		}

		GameNetworkManager.Instance.KickPlayer( player.Connection, "Suspicious kill rate detected." );
	}
}
