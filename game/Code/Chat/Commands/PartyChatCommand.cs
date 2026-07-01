using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

/// <summary>
/// Sends a message to the caller's party channel (<see cref="MessageType.PartyChat"/>).
/// Slash-command entry point for the Tab-selectable PARTY chat mode, matching the
/// framework's other channel commands.
/// </summary>
public class PartyChatCommand : ICommand
{
	public string Command => "partychat";

	public string[] Aliases => new[]
	{
		"pchat"
	};

	public string Help => Language.GetPhrase( "party.chat_help" );
	public bool IsUsableWhileRestricted => true;
	public bool IsUsableWhileDead => true;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		var system = PartySystem.Instance;
		if ( system is null )
		{
			caller.Error( Language.GetPhrase( "party.ui_unavailable" ) );
			return true;
		}

		// Single shared party-chat path (cooldown + moderation + party-only delivery) lives on
		// PartySystem so /party <message> shorthand and /pchat behave identically.
		system.HostSendPartyChat( caller, ExtractMessage( raw ) );
		return true;
	}

	private static string ExtractMessage( string raw )
	{
		var firstSpace = raw.IndexOf( ' ' );
		return firstSpace < 0 ? string.Empty : raw[(firstSpace + 1)..].Trim();
	}
}
