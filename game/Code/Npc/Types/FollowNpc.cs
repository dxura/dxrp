namespace Dxura.RP.Game.Types;

public class FollowNpc : Npc
{

	protected override void OnUpdateAi()
	{
		var target = FindTarget( null )?.GameObject;
		if ( target == null )
		{
			return;
		}

		UpdateMovement( target );
	}
}
