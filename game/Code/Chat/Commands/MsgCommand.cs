using System.Text;
using System.Text.RegularExpressions;

namespace Dxura.RP.Game.Commands;

public class MsgCommand : ICommand
{
	public const string Name = "msg";
	public string Command => Name;
	public string[] Aliases => new[]
	{
		"pm",
		"dm"
	};
	public string Help => $"Send a private message to another player. {UsageMessage}";
	public bool IsUsableWhileRestricted => true;

	private static string UsageMessage => Language.GetPhrase( "command.msg.usage" );

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:msg", Config.Current.Game.PrivateMessageCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		if ( args.Length < 2 )
		{
			caller.SendMessage( UsageMessage );
			return true;
		}

		// Parse the target player name and message
		var (targetName, message) = ParseTargetAndMessage( raw );

		if ( string.IsNullOrWhiteSpace( targetName ) || string.IsNullOrWhiteSpace( message ) )
		{
			caller.SendMessage( UsageMessage );
			return true;
		}

		if ( message.Length > Config.Current.Game.ChatMaxLength )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.msg.too_long" ), Config.Current.Game.ChatMaxLength ) );
			return true;
		}

		// Find matching players
		var matchingPlayers = GameUtils.GetPlayersByName( targetName );

		if ( matchingPlayers.Count == 0 )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.msg.not_found" ), targetName ) );
			return true;
		}

		if ( matchingPlayers.Count > 1 )
		{
			// Multiple matches found, show list
			var playerNames = string.Join( ", ", matchingPlayers.Select( p => p.DisplayName ) );
			caller.SendMessage( string.Format( Language.GetPhrase( "command.msg.multiple" ), targetName, playerNames ) );
			return true;
		}

		var targetPlayer = matchingPlayers[0];

		// Send the private message
		SendPrivateMessage( caller, targetPlayer, message );

		return true;
	}

	private (string targetName, string message) ParseTargetAndMessage( string raw )
	{
		// Find the first space after the command to get the content
		var firstSpaceIndex = raw.IndexOf( ' ' );
		if ( firstSpaceIndex == -1 )
		{
			return (string.Empty, string.Empty);
		}

		var content = raw.Substring( firstSpaceIndex + 1 ).TrimStart();

		if ( string.IsNullOrWhiteSpace( content ) )
		{
			return (string.Empty, string.Empty);
		}

		// Check if the target name is quoted
		if ( content.StartsWith( "\"" ) )
		{
			var endQuoteIndex = content.IndexOf( '"', 1 );
			if ( endQuoteIndex == -1 )
			{
				// No closing quote found
				return (string.Empty, string.Empty);
			}

			var targetName = content.Substring( 1, endQuoteIndex - 1 );
			var message = content.Substring( endQuoteIndex + 1 ).TrimStart();

			return (targetName, message);
		}
		else
		{
			// Split on first space
			var spaceIndex = content.IndexOf( ' ' );
			if ( spaceIndex == -1 )
			{
				// No space found, treat entire content as target name
				return (content, string.Empty);
			}

			var targetName = content.Substring( 0, spaceIndex );
			var message = content.Substring( spaceIndex + 1 );

			return (targetName, message);
		}
	}

	private void SendPrivateMessage( Player sender, Player recipient, string message )
	{
		// Send to sender (showing what they sent)
		using ( Rpc.FilterInclude( c => c.Id == sender.ConnectionId ) )
		{
			Chat.Current.BroadcastChat(
				string.Format( Language.GetPhrase( "command.msg.sent" ), recipient.DisplayName )
			);
		}

		// Send to recipient (showing who sent it)
		using ( Rpc.FilterInclude( c => c.Id == recipient.ConnectionId ) )
		{
			Chat.Current.BroadcastPlayerChat(
				Guid.NewGuid(),
				sender.ConnectionId,
				message,
				MessageType.PrivateMessage
			);
		}

		Log.Info( $"[PM] {sender.DisplayName} -> {recipient.DisplayName}: {message}" );

		// Log to Discord
		_ = ServerApiClient.Audit( "PrivateMessage", $"{sender.SteamName} ({sender.SteamId}) -> {recipient.SteamName} ({recipient.SteamId}): {message}", sender.SteamId );

		// Track this PM for both sides so /r works immediately for the sender too.
		ReplyCommand.SetLastPmSender( recipient.ConnectionId, sender.ConnectionId );
		ReplyCommand.SetLastPmSender( sender.ConnectionId, recipient.ConnectionId );
	}
}
