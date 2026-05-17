using Sandbox.Services;

namespace Dxura.RP.Game;

/// <summary>
///     Achievement System which tracks gameplay session duration and awards achievements at specific time thresholds.
///     Might do other more stuff in future
/// </summary>
public class AchievementSystem : Component, IGameEvents
{
	// Achievement definitions with time thresholds and IDs
	private readonly Dictionary<string, float> _sessionAchievements = new()
	{
		{
			"play_session_30m", 1800f
		}, // 30 minutes
		{
			"play_session_1h", 3600f
		}, // 1 hour
		{
			"play_session_2h", 7200f
		}, // 2 hours
		{
			"play_session_4h", 14400f
		}, // 4 hours
		{
			"play_session_8h", 28800f
		} // 8 hours
	};

	// Track unlocked achievements
	private readonly HashSet<string> _unlockedAchievements = new();

	// Track session time
	private TimeSince _sessionTime;

	protected override void OnStart()
	{
		_sessionTime = 0;
	}

	public void OnSecondlyUpdate()
	{
		CheckSessionAchievements();
	}


	private void CheckSessionAchievements()
	{
		foreach ( var (id, threshold) in _sessionAchievements )
		{
			// If this achievement has been unlocked yet or the time threshold is not met
			if ( _unlockedAchievements.Contains( id ) || _sessionTime < threshold )
			{
				continue;
			}

			// Award it
			Achievements.Unlock( id );
			_unlockedAchievements.Add( id );
		}
	}
}
