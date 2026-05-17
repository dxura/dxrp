using Dxura.RP.Shared;
namespace Dxura.RP.Game.Minigame;

public partial class MinigameSystem
{
	[Property] [Group( "Effects" )] public SoundEvent? MinigameCountdown { get; set; }
	[Property] [Group( "Effects" )] public SoundEvent? MinigameOver { get; set; }


	private bool _countdownPlayed;

	private void OnSecondlyUpdateEffects()
	{
		if ( !_countdownPlayed && CurrentState == MinigameState.PreLobby && _timeSinceStateChange > CurrentMinigame?.LobbyDuration - 3f )
		{
			DoMinigameCountdownEffects();
		}
	}

	private void DoMinigameCountdownEffects()
	{
		MinigameCountdown?.BroadcastHost( _lobbyPosition );
		_countdownPlayed = true;
	}

	private void DoMinigameOverEffects()
	{
		MinigameOver?.BroadcastHost( _lobbyPosition );
	}

	private void ClearEffects()
	{
		_countdownPlayed = false;
	}

}
