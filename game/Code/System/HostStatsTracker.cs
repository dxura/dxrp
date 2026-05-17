using Dxura.RP.Shared;
namespace Dxura.RP.Game;

/// <summary>
/// Tracks server host stats (FPS, bandwidth) for reporting via the API pulse.
/// </summary>
public class HostStatsTracker : GameObjectSystem<HostStatsTracker>
{
	private int _frameCount;
	private float _frameTimeAccum;
	private TimeSince _lastSample = 0;

	private ushort _fps;
	private ushort _avgFps;
	private ushort _minFps = ushort.MaxValue;

	private const float Smoothing = 0.1f;

	public HostStatsTracker( Scene scene ) : base( scene )
	{
		Listen( Stage.FinishUpdate, 0, Track, "Track Host Stats" );
	}

	private void Track()
	{
		if ( !Networking.IsHost || Scene.IsEditor || !Networking.IsActive )
			return;

		_frameCount++;
		_frameTimeAccum += RealTime.Delta;

		if ( _lastSample < 1f )
			return;

		_fps = (ushort)(_frameCount / _frameTimeAccum).CeilToInt();
		_frameCount = 0;
		_frameTimeAccum = 0;
		_lastSample = 0;

		_avgFps = _avgFps == 0 ? _fps : (ushort)(_avgFps * (1f - Smoothing) + _fps * Smoothing);
		_minFps = Math.Min( _minFps, _fps );
	}

	public ServerHostStatsDto GetStats()
	{
		var connections = Connection.All.Where( c => c != Connection.Local ).ToArray();

		var totalBytesIn = 0f;
		var totalBytesOut = 0f;

		foreach ( var c in connections )
		{
			var s = c.Stats;
			totalBytesIn += s.InBytesPerSecond;
			totalBytesOut += s.OutBytesPerSecond;
		}

		var stats = new ServerHostStatsDto
		{
			InBytesPerSecond = totalBytesIn,
			OutBytesPerSecond = totalBytesOut,
			Fps = _fps,
			Avg30SecFps = _avgFps,
			Min30SecFps = _minFps
		};

		_minFps = _fps;

		return stats;
	}
}
