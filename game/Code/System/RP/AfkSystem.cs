using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

public class AfkSystem : SingletonComponent<AfkSystem>, IGameEvents
{
	private const float SecondsPerMinute = 60f;
	
	private Dictionary<long, int> _lastPlayerInputActivitySequences = new();
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
		var currentPlayerInputActivitySequences = new Dictionary<long, int>();

		foreach ( var player in GameUtils.Players )
		{
			if ( !player.IsValid() )
			{
				continue;
			}

			var currentInputActivitySequence = player.AfkInputActivitySequence;
			currentPlayerInputActivitySequences[player.SteamId] = currentInputActivitySequence;

			if ( !_lastPlayerInputActivitySequences.TryGetValue( player.SteamId, out var lastInputActivitySequence ) )
			{
				// First time seeing this player, initialize their idle timer
				_playerIdleTime[player.SteamId] = 0f;
				continue;
			}

			var hasInputActivity = currentInputActivitySequence != lastInputActivitySequence;

			if ( !hasInputActivity )
			{
				// Player did not provide real owner input since the last check.
				_playerIdleTime.TryAdd( player.SteamId, 0f );

				var idleTime = _playerIdleTime[player.SteamId];

				// Only add AFK status if they've been idle for X minutes
				if ( idleTime >= config.TimeUntilAfk )
				{
					player.AddStatus( Constants.AfkStatus );
				}

				// Demote to Citizen if AFK for more than 60 minutes
				if ( config.AfkDemoteEnabled && idleTime >= config.TimeUntilAfkDemote )
				{
					if ( !player.Job.IsCitizenRole() && player.Job.Selectable )
					{
						player.AssignJobHost( GameModeJobs.GetByTagOrFallback( JobTag.Citizen, "Citizen" ) );
						var minutesAfk = config.TimeUntilAfkDemote / SecondsPerMinute;
						player.SendMessage( $"You have been demoted to Citizen due to being AFK for {minutesAfk} minutes." );
					}
				}
			}
			else
			{
				// Player used keyboard/mouse input, reset their idle timer and remove AFK status.
				_playerIdleTime[player.SteamId] = 0f;
				player.RemoveStatus( Constants.AfkStatus );
			}
		}

		_lastPlayerInputActivitySequences = currentPlayerInputActivitySequences;
	}

	public void ForceAfk( Player player )
	{
		Assert.True( Networking.IsHost );

		if ( !player.IsValid() )
		{
			return;
		}

		player.AddStatus( Constants.AfkStatus );
		_lastPlayerInputActivitySequences[player.SteamId] = player.AfkInputActivitySequence;
		_playerIdleTime[player.SteamId] = 0f;
	}

	public void OnPlayerDisconnectHost( long steamId )
	{
		_lastPlayerInputActivitySequences.Remove( steamId );
		_playerIdleTime.Remove( steamId );
	}
}
