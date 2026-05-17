using Dxura.RP.Game.UI;
using Dxura.RP.Shared;
using System.Text.RegularExpressions;

namespace Dxura.RP.Game.Commands;

public class StaffAnnounceCommand : ICommand
{
	public static string Name => "staffannounce";
	public string Command => Name;
	public string Help => "Send a server-wide staff announcement (Staff only)";
	public Permission[] RequiredPermissions => [Permission.ServerBroadcast];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( args.Length == 0 || string.IsNullOrWhiteSpace( string.Join( " ", args ) ) )
		{
			caller.Error( "#announce.no_message" );
			return true;
		}

		var message = string.Join( " ", args );

		// Basic text moderation - remove excessive whitespace and trim
		message = Regex.Replace( message, @"\s+", " " ).Trim();

		// Length limit
		if ( message.Length > Config.Current.Game.MayorAnnounceMaxLength )
		{
			caller.Error( "#announce.too_long" );
			return true;
		}

		message = GameManager.ModerateText( caller.SteamId, "STAFF ANNOUNCE", message );

		// Broadcast announcement to all players
		GameManager.Instance.BroadcastAnnouncementHost( message, Announcement.AnnouncementType.Staff );

		_ = ServerApiClient.Audit( "StaffAnnounce", $"{caller.SteamName} ({caller.SteamId}): {message}", caller.SteamId );

		return true;
	}
}
