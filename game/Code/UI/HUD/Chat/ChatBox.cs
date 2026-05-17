namespace Dxura.RP.Game.UI;

internal class ChatBox : TextEntry
{
	public Action? OnTabPressed { get; set; }
	public Action? OnHistoryPrevious { get; set; }
	public Action? OnHistoryNext { get; set; }

	public override void OnButtonTyped( ButtonEvent e )
	{
		var button = e.Button;

		if ( button.Equals( "up", StringComparison.OrdinalIgnoreCase ) )
		{
			e.StopPropagation = true;
			OnHistoryPrevious?.Invoke();
			return;
		}

		if ( button.Equals( "down", StringComparison.OrdinalIgnoreCase ) )
		{
			e.StopPropagation = true;
			OnHistoryNext?.Invoke();
			return;
		}

		if ( button.Equals( "tab", StringComparison.OrdinalIgnoreCase ) )
		{
			e.StopPropagation = true;
			OnTabPressed?.Invoke();
			return;
		}

		base.OnButtonTyped( e );
	}
}
