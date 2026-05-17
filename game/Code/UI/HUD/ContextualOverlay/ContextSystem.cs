using Dxura.RP.Game.UI;

namespace Dxura.RP.Game;

public class ContextSystem : Panel
{
	private Dictionary<IContextualObject, Context> ActiveContextOverlays { get; set; } = new();

	private void Refresh()
	{
		var deleteList = new List<IContextualObject>();
		deleteList.AddRange( ActiveContextOverlays.Keys );

		foreach ( var contextualObject in Scene.GetAllComponents<IContextualObject>() )
		{
			if ( UpdateContexts( contextualObject ) )
			{
				deleteList.Remove( contextualObject );
			}
		}

		foreach ( var contextualObject in deleteList )
		{
			ActiveContextOverlays[contextualObject].Delete();
			ActiveContextOverlays.Remove( contextualObject );
		}
	}

	private Context CreateContext( IContextualObject contextual )
	{
		var inst = new Context { Object = contextual };
		AddChild( inst );
		return inst;
	}

	private bool UpdateContexts( IContextualObject contextual )
	{
		if ( !contextual.GameObject.IsValid() || contextual.GameObject.Tags.Has( Constants.OccludeTag ) )
		{
			return false;
		}

		if ( !contextual.ShouldShow() )
		{
			return false;
		}

		var player = Player.Local;
		var camera = Scene.Camera;
		if ( !camera.IsValid() || !player.IsValid() )
		{
			return false;
		}

		if ( contextual.ContextMaxDistance != 0f && player.WorldPosition.Distance( contextual.ContextPosition ) >
		    contextual.ContextMaxDistance )
		{
			return false;
		}

		if ( !ActiveContextOverlays.TryGetValue( contextual, out var instance ) )
		{
			instance = CreateContext( contextual );
			if ( instance.IsValid() )
			{
				ActiveContextOverlays[contextual] = instance;
			}
		}

		instance.Reposition();

		return true;
	}

	public override void Tick()
	{
		Refresh();
	}
}
