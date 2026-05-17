using Dxura.RP.Game.Minigame.Minigames;
namespace Dxura.RP.Game.System.Minigame;

public interface IMinigame
{
	GameObject? GameObject { get; }

	void Initialize( MinigameResource resource );

	void End();

	List<Transform> GetMinigameSpawnPoints();
}
