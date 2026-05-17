namespace Dxura.RP.Game.UI;

internal class TabMenuTextEntry : TextEntry
{
	public override void OnButtonTyped( ButtonEvent e )
	{
		if ( e.Button.Equals( "tab", StringComparison.OrdinalIgnoreCase ) )
		{
			e.StopPropagation = true;
			TabMenu.RequestCloseMenu();
			return;
		}

		base.OnButtonTyped( e );
	}
}
