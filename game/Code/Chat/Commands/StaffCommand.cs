using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class StaffCommand : ICommand
{
	public string Command => "staff";
	public string Help => Language.GetPhrase( "command.staff.help" );
	public bool IsUsableWhileRestricted => true;
	public bool IsUsableWhileDead => true;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		Log.Info( $"Player {caller.SteamId} requested staff ({raw})" );

		// Extract message from the command
		var message = raw.Length > 6 ? raw.Substring( 6 ).Trim() : "";
		if ( string.IsNullOrWhiteSpace( message ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.staff.provide_message" ) );
			return true;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:staff", Config.Current.Game.StaffCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		var hasAvailableStaff = HasAvailableStaff();
		var created = AdminTicketSystem.Current?.CreateOrAppendTicketHost( caller, message ) ?? true;

		// Confirm to the caller
		caller.SendMessage( Language.GetPhrase( created ? "command.staff.sent" : "command.staff.updated" ) );
		
		if ( !hasAvailableStaff )
		{
			caller.SendMessage( Language.GetPhrase( "command.staff.no_available" ) );
		}

		return true;
	}

	private static bool HasAvailableStaff()
	{
		return GameUtils.Players
			.Where( p => p.IsValid() && RankSystem.HasPermission( p.SteamId, Permission.HandleTickets ) )
			.Where( p => !Config.Current.Game.AfkEnabled || !p.HasStatus( Constants.AfkStatus ) )
			.Any();
	}
}
