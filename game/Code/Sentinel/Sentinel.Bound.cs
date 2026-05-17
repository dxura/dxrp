using Dxura.RP.Game.Minigame;
namespace Dxura.RP.Game.Sentinel;

public partial class Sentinel
{
	private TimeSince _lastProcessMapBound = 0;
	private TimeSince _lastProcessPlayerRayBound = 0;

	private int _currentRayPlayerIndex;

	public MapInstance? CurrentMap { get; set; }

	private void ProcessBound()
	{
		if ( !Config.Current.Game.SentinelBoundEnabled )
		{
			return;
		}

		if ( _lastProcessMapBound > 1 )
		{
			DetectMapBoundViolations();
			_lastProcessMapBound = 0;
		}

		// if ( _lastProcessPlayerRayBound > 5 )
		// {
		// 	DetectPlayerRayBoundViolations();
		// 	_lastProcessPlayerRayBound = 0;
		// }
	}

	private void DetectPlayerRayBoundViolations()
	{
		var players = GameUtils.Players.ToList();

		if ( players.Count == 0 )
		{
			_currentRayPlayerIndex = 0;
			return;
		}

		// Wrap around to start if we've exceeded player count
		if ( _currentRayPlayerIndex >= players.Count )
		{
			_currentRayPlayerIndex = 0;
		}

		var player = players[_currentRayPlayerIndex];
		_currentRayPlayerIndex++;

		var isDead = player.HealthComponent.State != LifeState.Alive;
		var lastRespawn = player.TimeSinceLastRespawn;
		var isInMinigame = MinigameSystem.Instance.IsValid() && MinigameSystem.Instance.IsPlayerInMinigame( player );

		if ( IsExempt( player ) || lastRespawn < 3 || isDead || isInMinigame )
		{
			return;
		}

		if ( !player.Controller.IsValid() )
		{
			return;
		}

		var eyePos = player.Controller.EyePosition;
		var directions = new[]
		{
			Vector3.Forward,
			Vector3.Backward,
			Vector3.Left,
			Vector3.Right
		};

		var allHit = (from dir in directions
			select Scene.Trace.Ray( eyePos, eyePos + dir * 10000f )
				.WithTag( Constants.MapTag )
				.Run()).All( trace => trace.Hit );

		if ( !allHit )
		{
			DisciplineBound( player );
		}
	}

	private void DetectMapBoundViolations()
	{
		if ( !CurrentMap.IsValid() )
		{
			return;
		}

		foreach ( var player in GameUtils.Players )
		{
			var isDead = player.HealthComponent.State != LifeState.Alive;
			var lastRespawn = player.TimeSinceLastRespawn;
			var isInMinigame = MinigameSystem.Instance.IsValid() && MinigameSystem.Instance.IsPlayerInMinigame( player );

			if ( IsExempt( player ) || lastRespawn < 3 || isDead || isInMinigame )
			{
				continue;
			}

			if ( !CurrentMap.Bounds.Contains( player.WorldPosition ) )
			{
				DisciplineBound( player );
			}
		}
	}

	private void DisciplineBound( Player player )
	{
		var config = Config.Current.Game;

		RecordViolation( player, "Out of Bound", typeReportingEnabled: config.SentinelBoundReportingEnabled );

		if ( !config.SentinelBoundPunishmentEnabled )
		{
			return;
		}

		player.KillHost();
	}
}
