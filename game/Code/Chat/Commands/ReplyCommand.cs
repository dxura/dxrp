namespace Dxura.RP.Game.Commands;

public class ReplyCommand : ICommand
{
	public const string Name = "reply";
	public string[] Aliases => new[]
	{
		"r"
	};
	public string Command => Name;
	public string Help => "Reply to the last private message. Usage: /r <message>";

	private static readonly Dictionary<Guid, Guid> LastPmSenders = new();

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:reply", Config.Current.Game.PrivateMessageCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		if ( args.Length == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.reply.usage" ) );
			return true;
		}

		// Get the last sender
		if ( !LastPmSenders.TryGetValue( caller.ConnectionId, out var lastSenderId ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.reply.no_sender" ) );
			return true;
		}

		var lastSender = GameUtils.GetPlayerByConnectionId( lastSenderId );
		if ( !lastSender.IsValid() )
		{
			caller.SendMessage( Language.GetPhrase( "command.reply.offline" ) );
			return true;
		}

		// Get the message content
		var firstSpaceIndex = raw.IndexOf( ' ' );
		if ( firstSpaceIndex == -1 )
		{
			caller.SendMessage( Language.GetPhrase( "command.reply.usage" ) );
			return true;
		}

		var message = raw.Substring( firstSpaceIndex + 1 ).TrimStart();

		if ( string.IsNullOrWhiteSpace( message ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.reply.usage" ) );
			return true;
		}

		if ( message.Length > Config.Current.Game.ChatMaxLength )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.msg.too_long" ), Config.Current.Game.ChatMaxLength ) );
			return true;
		}

		// Send the reply using the same logic as MsgCommand
		SendPrivateMessage( caller, lastSender, message );

		return true;
	}

	public static void SetLastPmSender( Guid recipientId, Guid senderId )
	{
		LastPmSenders[recipientId] = senderId;
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

		// Track this PM for both sides so /r keeps following the active DM target.
		SetLastPmSender( recipient.ConnectionId, sender.ConnectionId );
		SetLastPmSender( sender.ConnectionId, recipient.ConnectionId );
	}
}
