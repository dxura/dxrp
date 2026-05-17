namespace Dxura.RP.Game.Minigame;

public partial class MinigameSystem
{
	[Sync( SyncFlags.FromHost )]
	public TimeUntil NextAutoMinigameTime { get; private set; } = 0;

	private void OnStartAuto()
	{
		NextAutoMinigameTime = Config.Current.Game.MinigameAutoInterval;
	}

	private void OnSecondlyUpdateAuto()
	{
		if ( !Networking.IsHost || !Config.Current.Game.MinigamesAutoEnabled )
		{
			return;
		}

		if ( IsMinigameActive() )
		{
			NextAutoMinigameTime = Config.Current.Game.MinigameAutoInterval;
			return;
		}

		if ( NextAutoMinigameTime > 0 )
		{
			return;
		}

		NextAutoMinigameTime = Config.Current.Game.MinigameAutoInterval;

		// Check min players
		if ( GameUtils.GetActivePlayerCount() < Config.Current.Game.MinigameAutoMinPlayers )
		{
			Log.Info( "[Minigame] Not enough players to auto start one." );
			return;
		}

		StartMinigame();
	}

}
