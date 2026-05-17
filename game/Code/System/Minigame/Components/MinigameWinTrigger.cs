using Dxura.RP.Game.Minigame;
namespace Dxura.RP.Game;

public class MinigameWinTrigger : Component, Component.ITriggerListener
{

	public void OnTriggerEnter( Collider other )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		var minigameSystem = MinigameSystem.Instance;
		if ( minigameSystem.CurrentState != MinigameState.Playing )
		{
			return;
		}

		var target = other.GameObject.Root;

		// Check if the object is a player
		if ( !target.Tags.Has( Constants.PlayerTag ) )
		{
			return;
		}

		var player = target.GetComponent<Player>();
		minigameSystem.NotifyWin( player );
	}
}
