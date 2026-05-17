namespace Dxura.RP.Game;

/// <summary>
/// Static helper for showing warning overlays to players
/// </summary>
public static class WarningOverlayHelper
{
	/// <summary>
	/// Show a warning overlay to a specific player
	/// </summary>
	/// <param name="player">The player to show the warning to</param>
	/// <param name="title">The warning title</param>
	/// <param name="message">The warning message</param>
	/// <param name="confirmText">Optional confirm button text (defaults to "Acknowledge")</param>
	public static void ShowWarning( Player player, string title, string message, string confirmText = "#warning.overlay.confirm" )
	{
		if ( !player.IsValid() )
		{
			return;
		}

		using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
		{
			BroadcastShowWarning( player.SteamId, title, message, confirmText );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private static void BroadcastShowWarning( long playerId, string title, string message, string confirmText )
	{
		if ( !Player.Local.IsValid() || !GameManager.Instance.IsValid() )
		{
			return;
		}

		// If playerId is 0, show to everyone. Otherwise, only show to the specific player
		if ( playerId != 0 && Player.Local.SteamId != playerId )
		{
			return;
		}

		var warningOverlay = GameManager.Instance.HudGameObject.AddComponent<WarningOverlay>();
		warningOverlay.Title = title;
		warningOverlay.Message = message;
		warningOverlay.ConfirmText = confirmText;
	}
}
