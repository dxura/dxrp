using Dxura.RP.Game.System.Minigame;
namespace Dxura.RP.Game.Minigame.Minigames;

public class Minigame : Component, IMinigame
{
	public MinigameResource Resource = null!;

	private List<Transform> _minigameSpawnPoints = new();

	public void Initialize( MinigameResource resource )
	{
		Resource = resource;
		_minigameSpawnPoints = GameObject.GetComponentsInChildren<MinigameSpawnPoint>().Select( x => new Transform( x.WorldPosition, x.WorldRotation ) ).ToList();
	}

	public void End()
	{
		GameObject.Destroy();
	}

	public List<Transform> GetMinigameSpawnPoints()
	{
		return _minigameSpawnPoints;
	}
}
