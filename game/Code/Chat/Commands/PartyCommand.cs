using System.Globalization;
using Dxura.RP.Game.UI;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

/// <summary>
/// Player-facing party command. Runs host-side mutations through <see cref="PartySystem"/>.
/// Bare <c>/party</c> (or <c>/p</c>) opens the management UI client-side; a recognized subcommand runs
/// host-side; anything else is treated as a party-chat message (e.g. <c>/p on my way</c>).
/// </summary>
public class PartyCommand : ICommand
{
	public string Command => "party";

	// /p is the party alias (the old /p → profile alias moved to /pf). Bare /p opens the menu, /p <msg>
	// is party-chat shorthand, and /p <subcommand> runs the matching party action.
	public string[] Aliases => ["p"];
	public string Help => Language.GetPhrase( "party.command_help" );

	/// <summary>
	/// Opening the management menu is a purely client-side concern, so bare <c>/party</c> and
	/// <c>/party menu</c> toggle it here (before the host send) and consume the command. Every other
	/// subcommand returns false and falls through to <see cref="ExecuteHost"/> as before.
	/// </summary>
	public bool ExecuteLocal( string[] args, string raw )
	{
		if ( args.Length == 0 || args[0].Equals( "menu", StringComparison.OrdinalIgnoreCase ) )
		{
			PartyMenu.Toggle();
			return true;
		}

		return false;
	}

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandParty ) )
		{
			caller.Error( "#generic.no_permission" );
			return true;
		}

		var system = PartySystem.Instance;
		if ( system is null )
		{
			caller.Error( Language.GetPhrase( "party.ui_unavailable" ) );
			return true;
		}

		if ( args.Length == 0 )
		{
			// Bare /party opens the client-side management menu via ExecuteLocal and never reaches the
			// host normally; this remains as a safe host-side fallback (e.g. a client with no menu).
			system.HostInfo( caller );
			return true;
		}

		switch ( args[0].ToLowerInvariant() )
		{
			case "invite":
				if ( !TryResolveTarget( caller, args, out var inviteTarget ) )
				{
					return true;
				}

				system.HostInvite( caller, inviteTarget! );
				return true;

			case "kick":
				if ( !TryResolveTarget( caller, args, out var kickTarget ) )
				{
					return true;
				}

				system.HostKick( caller, kickTarget! );
				return true;

			case "promote":
				if ( !TryResolveTarget( caller, args, out var promoteTarget ) )
				{
					return true;
				}

				system.HostPromote( caller, promoteTarget! );
				return true;

			case "accept":
				system.HostAccept( caller );
				return true;

			case "leave":
				system.HostLeave( caller );
				return true;

			case "disband":
				system.HostDisband( caller );
				return true;

			case "info":
				system.HostInfo( caller );
				return true;

			case "color":
				if ( args.Length < 2 )
				{
					caller.SendMessage( Language.GetPhrase( "party.color_usage" ) );
					return true;
				}

				if ( !TryParseHexColor( args[1], out var color ) )
				{
					caller.Error( Language.GetPhrase( "party.color_invalid" ) );
					return true;
				}

				system.HostSetColor( caller, color );
				return true;

			case "outline":
				if ( args.Length < 2 )
				{
					caller.SendMessage( Language.GetPhrase( "party.outline_usage" ) );
					return true;
				}

				if ( !TryParseOnOff( args[1], out var outlineEnabled ) )
				{
					caller.Error( Language.GetPhrase( "party.outline_invalid" ) );
					return true;
				}

				system.HostSetMemberOutline( caller, outlineEnabled );
				return true;

			default:
				// Shorthand: anything that isn't a known subcommand is treated as a party-chat message
				// (e.g. "/party lets push north", "/p on my way"). Routes through the same shared,
				// cooldown-checked path as /pchat. Bare /party and /party menu are consumed in ExecuteLocal.
				system.HostSendPartyChat( caller, string.Join( ' ', args ) );
				return true;
		}
	}

	private static bool TryResolveTarget( Player caller, string[] args, out Player? target )
	{
		target = null;

		if ( args.Length < 2 )
		{
			caller.SendMessage( Language.GetPhrase( "party.command_usage" ) );
			return false;
		}

		target = CommandHelper.ResolvePlayer( caller, args[1] );
		return target.IsValid();
	}

	/// <summary>
	/// Parses a 6-digit hex color (with or without a leading '#') into a packed 0xRRGGBB value.
	/// </summary>
	private static bool TryParseHexColor( string input, out uint packed )
	{
		packed = 0;

		if ( string.IsNullOrWhiteSpace( input ) )
		{
			return false;
		}

		var hex = input.Trim().TrimStart( '#' );
		if ( hex.Length != 6 )
		{
			return false;
		}

		return uint.TryParse( hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out packed );
	}

	private static bool TryParseOnOff( string input, out bool enabled )
	{
		enabled = false;
		if ( string.IsNullOrWhiteSpace( input ) )
		{
			return false;
		}

		switch ( input.Trim().ToLowerInvariant() )
		{
			case "on":
			case "true":
			case "1":
			case "yes":
				enabled = true;
				return true;
			case "off":
			case "false":
			case "0":
			case "no":
				enabled = false;
				return true;
			default:
				return false;
		}
	}
}
