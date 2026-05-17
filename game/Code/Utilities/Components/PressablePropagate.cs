namespace Dxura.RP.Game;

[Title( "Pressable Propagate" )]
[Category( "DXRP" )]
public class PressablePropagate : Component, Component.IPressable
{
	private IPressable? Parent;
	private bool _loggedMissingParent;

	protected override void OnStart()
	{
		ResolveParent();
	}

	public bool CanPress( IPressable.Event e )
	{
		return ResolveParent()?.CanPress( e ) ?? false;
	}

	public bool Press( IPressable.Event e )
	{
		return ResolveParent()?.Press( e ) ?? false;
	}

	public void Hover( IPressable.Event e )
	{
		ResolveParent()?.Hover( e );
	}

	public void Look( IPressable.Event e )
	{
		ResolveParent()?.Look( e );
	}

	public void Blur( IPressable.Event e )
	{
		ResolveParent()?.Blur( e );
	}

	public bool Pressing( IPressable.Event e )
	{
		return ResolveParent()?.Pressing( e ) ?? false;
	}

	public void Release( IPressable.Event e )
	{
		ResolveParent()?.Release( e );
	}

	public IPressable.Tooltip? GetTooltip( IPressable.Event e )
	{
		return ResolveParent()?.GetTooltip( e );
	}

	private IPressable? ResolveParent()
	{
		if ( Parent != null )
		{
			return Parent;
		}

		Parent = GameObject.Root.GetComponent<IPressable>();

		if ( Parent != null )
		{
			_loggedMissingParent = false;
			return Parent;
		}

		if ( !_loggedMissingParent )
		{
			Log.Warning( $"PressablePropagate on {GameObject} could not find a parent IPressable component." );
			_loggedMissingParent = true;
		}

		return null;
	}
}
