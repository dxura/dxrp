using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

public class AfkSystem : SingletonComponent<AfkSystem>, IGameEvents
{
	private const float SecondsPerMinute = 60f;
	
	private Dictionary<long, Vector3> _lastPlayerPositions = new();
	private readonly Dictionary<long, TimeSince> _playerIdleTime = new();

	private TimeSince _timeSinceLastAfkCheck = 0f;

	protected override void OnStart()
	{
		if ( !Config.Current.Game.AfkEnabled )
		{
			Destroy();
		}
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( _timeSinceLastAfkCheck < Config.Current.Game.AfkCheckInterval )
		{
			return;
		}

		_timeSinceLastAfkCheck = 0f;

		var config = Config.Current.Game;
		var currentPlayerPositions = new Dictionary<long, Vector3>();

		foreach ( var player in GameUtils.Players )
		{
			if ( !player.IsValid() )
			{
				continue;
			}

			var currentPosition = player.WorldPosition;
			currentPlayerPositions[player.SteamId] = currentPosition;

			if ( !_lastPlayerPositions.TryGetValue( player.SteamId, out var lastPosition ) )
			{
				// First time seeing this player, initialize their idle timer
				_playerIdleTime[player.SteamId] = 0f;
				continue;
			}

			var distanceMoved = lastPosition.Distance( currentPosition );

			if ( distanceMoved <= config.AfkMovementThreshold )
			{
				// Player hasn't moved enough
				_playerIdleTime.TryAdd( player.SteamId, 0f );

				var idleTime = _playerIdleTime[player.SteamId];

				// Only add AFK status if they've been idle for X minutes
				if ( idleTime >= Config.Current.Game.TimeUntilAfk )
				{
					player.AddStatus( Constants.AfkStatus );
				}

				// Demote to Citizen if AFK for more than 60 minutes
				if ( Config.Current.Game.AfkDemoteEnabled && idleTime >= Config.Current.Game.TimeUntilAfkDemote )
				{
					if ( !player.Job.IsCitizenRole() && player.Job.Selectable )
					{
						player.AssignJobHost( GameModeJobs.GetByTagOrFallback( JobTag.Citizen, "Citizen" ) );
						var minutesAfk = Config.Current.Game.TimeUntilAfkDemote / SecondsPerMinute;
						player.SendMessage( $"You have been demoted to Citizen due to being AFK for {minutesAfk} minutes." );
					}
				}
			}
			else
			{
				// Player moved, reset their idle timer and remove AFK status
				_playerIdleTime[player.SteamId] = 0f;
				player.RemoveStatus( Constants.AfkStatus );
			}
		}

		_lastPlayerPositions = currentPlayerPositions;
	}

	public void ForceAfk( Player player )
	{
		Assert.True( Networking.IsHost );

		if ( !player.IsValid() )
		{
			return;
		}

		player.AddStatus( Constants.AfkStatus );
		_lastPlayerPositions.Remove( player.SteamId );
		_playerIdleTime.Remove( player.SteamId );
	}

	public void OnPlayerDisconnectHost( long steamId )
	{
		_lastPlayerPositions.Remove( steamId );
		_playerIdleTime.Remove( steamId );
	}
}
