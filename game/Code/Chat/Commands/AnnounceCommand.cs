using Dxura.RP.Game.UI;
using System.Text.RegularExpressions;

namespace Dxura.RP.Game.Commands;

public class AnnounceCommand : ICommand
{
	public static string Name => "announce";
	public string Command => Name;
	public string Help => "Send a city-wide announcement (Mayor only)";

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		// Check if player is mayor
		if ( !caller.Job.IsMayoralRole() )
		{
			caller.Error( "#announce.not_mayor" );
			return true;
		}

		if ( !Config.Current.Game.GovernanceMayorAnnounceEnabled )
		{
			caller.Error( Language.GetPhrase( "announce.disabled" ) );
			return true;
		}

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

		// Check cooldown
		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:announce", Config.Current.Game.MayorAnnounceCooldown ) )
		{
			caller.Error( "#announce.cooldown" );
			return true;
		}

		message = GameManager.ModerateText( caller.SteamId, "MAYOR ANNOUNCE", message );

		// Broadcast announcement to all players
		GameManager.Instance.BroadcastAnnouncementHost( message, Announcement.AnnouncementType.Mayor );

		_ = ServerApiClient.Audit( "MayorAnnounce", $"{caller.SteamName} ({caller.SteamId}): {message}", caller.SteamId );

		caller.UnlockAchievement( "announce" );

		return true;
	}
}
