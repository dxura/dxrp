using System;
using System.Collections.Generic;
using System.Linq;

namespace Dxura.RP.Game.Sentinel;

public partial class Sentinel
{
	private TimeSince _lastProcessNet = 0;
	private readonly Dictionary<long, NetSpamTracker> _netspamTrackers = new();

	private class NetSpamTracker
	{
		public int ViolationCount { get; set; }
		public int ConsecutiveHighSamples { get; set; }
		public float PeakBytesPerSecond { get; set; }
		public TimeSince LastViolation { get; set; }
		public TimeSince LastWarning { get; set; }
	}

	private void ProcessNet()
	{
		var config = Config.Current.Game;

		if ( !config.SentinelNetSpamEnabled || _lastProcessNet < config.SentinelNetSpamCheckInterval )
		{
			return;
		}
		_lastProcessNet = 0;

		CleanupDisconnectedTrackers();

		var samples = new List<(Player? player, Connection connection, long key, float bytesPerSecond)>();

		foreach ( var connection in Connection.All )
		{
			if ( connection.IsHost || !connection.IsActive )
			{
				continue;
			}

			var player = GameUtils.GetPlayerByConnectionId( connection.Id );
			long key = connection.SteamId;
			var isExempt = player.IsValid() && IsExempt( player );
			var connectionAge = DateTimeOffset.UtcNow - connection.ConnectionTime;

			if ( isExempt || connection.IsConnecting || connectionAge < TimeSpan.FromSeconds( config.SentinelNetSpamGracePeriod ) )
			{
				continue;
			}

			var bps = connection.Stats.InBytesPerSecond + connection.Stats.OutBytesPerSecond;
			samples.Add( (player, connection, key, bps) );

			if ( !_netspamTrackers.ContainsKey( key ) )
			{
				_netspamTrackers[key] = new NetSpamTracker();
			}
		}

		// Always check against the absolute minimum threshold
		var absoluteMinimum = config.SentinelNetSpamBandwidthThreshold;

		// Calculate statistical threshold if we have enough connections
		var threshold = absoluteMinimum;
		if ( samples.Count >= 3 )
		{
			var sorted = samples.Select( p => p.bytesPerSecond ).OrderBy( d => d ).ToList();
			var median = sorted[sorted.Count / 2];
			var q75 = sorted[(int)(sorted.Count * 0.75f)];

			threshold = Math.Max(
				Math.Max( median * 3f, q75 * 2f ),
				absoluteMinimum
			);
		}

		foreach ( var (player, connection, key, bps) in samples )
		{
			if ( bps > threshold )
			{
				ProcessNetViolation( player, connection, key, bps, threshold );
			}
			else
			{
				DecayNetViolations( key );
			}
		}
	}

	private void ProcessNetViolation( Player? player, Connection connection, long trackerKey, float bytesPerSecond, float threshold )
	{
		var tracker = _netspamTrackers[trackerKey];
		var config = Config.Current.Game;
		var burstThreshold = threshold * config.SentinelNetSpamBurstThresholdMultiplier;

		tracker.ConsecutiveHighSamples++;
		tracker.PeakBytesPerSecond = Math.Max( tracker.PeakBytesPerSecond, bytesPerSecond );

		if ( tracker.ConsecutiveHighSamples < config.SentinelNetSpamConsecutiveSampleThreshold &&
		     tracker.PeakBytesPerSecond < burstThreshold )
		{
			return;
		}

		tracker.ViolationCount++;
		tracker.LastViolation = 0;
		tracker.ConsecutiveHighSamples = 0;
		tracker.PeakBytesPerSecond = 0f;

		var identity = player.IsValid()
			? $"{player.DisplayName} ({player.SteamId})"
			: $"Connection {connection.Id} ({connection.Address})";

		var bpsKb = bytesPerSecond / 1024f;
		var thresholdKb = threshold / 1024f;
		var burstThresholdKb = burstThreshold / 1024f;
		var detail = $"Bandwidth: {bpsKb:F1} KB/s (Threshold: {thresholdKb:F1} KB/s, Burst: {burstThresholdKb:F1} KB/s), violations: {tracker.ViolationCount}";

		if ( tracker.ViolationCount >= config.SentinelNetSpamViolationCountThreshold )
		{
			if ( CanReportNetSpamViolations && player.IsValid() && tracker.LastWarning > 30f )
			{
				Log.Warning( $"[Sentinel] {identity} sustained netspam detected: {detail}" );
				tracker.LastWarning = 0;
			}
		}
		else if ( CanReportNetSpamViolations && tracker.LastWarning > 15f )
		{
			Log.Warning( $"[Sentinel] {identity} netspam warning: {detail}" );
			tracker.LastWarning = 0;
		}
	}

	private static bool CanReportNetSpamViolations =>
		CanReportViolations && Config.Current.Game.SentinelNetSpamReportingEnabled;

	private void DecayNetViolations( long trackerKey )
	{
		var tracker = _netspamTrackers[trackerKey];
		var config = Config.Current.Game;

		tracker.ConsecutiveHighSamples = 0;
		tracker.PeakBytesPerSecond = 0f;

		if ( tracker.LastViolation > config.SentinelNetSpamViolationDecayTime )
		{
			tracker.ViolationCount = Math.Max( 0, tracker.ViolationCount - 1 );
		}
	}

	private void CleanupDisconnectedTrackers()
	{
		var activeKeys = new HashSet<long>();

		foreach ( var connection in Connection.All )
		{
			if ( !connection.IsActive )
			{
				continue;
			}

			activeKeys.Add( (long)connection.SteamId );
		}

		var staleKeys = new List<long>();
		foreach ( var key in _netspamTrackers.Keys )
		{
			if ( !activeKeys.Contains( key ) )
			{
				staleKeys.Add( key );
			}
		}

		foreach ( var key in staleKeys )
		{
			_netspamTrackers.Remove( key );
		}
	}
}
