using Dxura.RP.Shared;
using Sandbox.Diagnostics;
using Sandbox.Speech;

namespace Dxura.RP.Game;

public sealed partial class Chat
{
	private const char ItemShareSeparator = '\u001f';
	private const float ItemShareCooldown = 30f;

	/// <summary>
	/// Called by UI on client to submit text. Handles local echo and RPC to host.
	/// </summary>
	public void SubmitPlayerChat( string? text, MessageType chatMode )
	{
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "chat", Config.Current.Game.ChatCooldown, true ) )
		{
			return;
		}

		// Support old-school global chat prefix
		if ( text.StartsWith( "//" ) )
		{
			text = text[2..];
			chatMode = MessageType.GlobalChat;
		}

		// Give commands a chance to run locally (e.g. UI-only commands) and short-circuit.
		if ( TryExecuteLocalCommand( text ) )
		{
			return;
		}

		ProcessPlayerChat( text, chatMode );
	}

	public void ShareInventoryItem( Guid itemDefinitionId )
	{
		if ( itemDefinitionId == Guid.Empty )
		{
			return;
		}

		ShareInventoryItemHost( itemDefinitionId );
	}

	[Rpc.Host]
	private async void ShareInventoryItemHost( Guid itemDefinitionId )
	{
		var callerId = Rpc.CallerId;
		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( player.HasStatus( Constants.GaggedStatus ) || Status.Current.HasStatus( player.SteamId, Constants.GaggedStatus ) )
		{
			player.SendMessage( "#chat.gagged.restrict" );
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:inventory-share", ItemShareCooldown ) )
		{
			return;
		}

		var inventory = await ServerApiClient.GetPlayerInventory( player.SteamId );
		var item = inventory?.FirstOrDefault( x => x.Definition.Id == itemDefinitionId && x.Quantity > 0 );
		if ( item == null )
		{
			player.Error( "You do not have that item." );
			return;
		}

		var message = BuildItemShareMessage( item );
		var messageId = Guid.NewGuid();
		var inRange = GetLocalChatRecipients( player );

		using ( Rpc.FilterInclude( c => inRange.Contains( c ) ) )
		{
			BroadcastPlayerChat( messageId, callerId, message, MessageType.ItemShare );
		}
	}

	[Rpc.Host]
	private void ProcessPlayerChat( string? message, MessageType messageType )
	{
		if ( string.IsNullOrWhiteSpace( message ) )
		{
			return;
		}

		var callerId = Rpc.CallerId;
		var callerSteamId = Rpc.Caller.SteamId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:chat", Config.Current.Game.ChatCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		// Gagged players can't use chat
		if ( player.HasStatus( Constants.GaggedStatus ) || Status.Current.HasStatus( player.SteamId, Constants.GaggedStatus ) )
		{
			player.SendMessage( "#chat.gagged.restrict" );
			return;
		}

		// Support faction chat shorthand: /f <message>
		if ( message.StartsWith( "/f ", StringComparison.OrdinalIgnoreCase ) )
		{
			message = message[3..];
			messageType = MessageType.FactionChat;
		}

		if ( HandlePlayerCommands( player, message ) )
		{
			return;
		}

		// Filter Message types that are allowed
		if ( messageType != MessageType.LocalChat &&
		     messageType != MessageType.GlobalChat &&
		     messageType != MessageType.GovernmentChat &&
		     messageType != MessageType.StaffChat &&
		     messageType != MessageType.FactionChat )
		{
			return;
		}

		message = message.Truncate( Config.Current.Game.ChatMaxLength );
		var messageId = Guid.NewGuid();

		// Let active statuses modify the message
		message = Status.Current.ModifyChat( player, message, messageType );

		message = GameManager.ModerateText( callerSteamId, $"CHAT {messageType}", message, true );

		switch ( messageType )
		{
			case MessageType.LocalChat:
				{
					// Server-side distance filter so modified clients can't eavesdrop on distant local chat.
					var inRange = GetLocalChatRecipients( player );

					using ( Rpc.FilterInclude( c => inRange.Contains( c ) ) )
					{
						BroadcastPlayerChat( messageId, callerId, message, MessageType.LocalChat );
					}

					break;
				}
			case MessageType.GovernmentChat:
				{
					if ( !player.Job.IsGovernmentRole() )
					{
						return;
					}

					var governmentPlayers = GameUtils.GetPlayersByJobTag( JobTag.Government )
						.Select( x => x.Connection )
						.ToHashSet();

					using ( Rpc.FilterInclude( c => governmentPlayers.Contains( c ) ) )
					{
						BroadcastPlayerChat( messageId, callerId, message, MessageType.GovernmentChat );
					}

					break;
				}
			case MessageType.StaffChat:
				{
					if ( !RankSystem.HasPermission( player.SteamId, Permission.StaffChat ) )
					{
						return;
					}

					var staffPlayers = GameUtils.Players
						.Where( x => x.IsValid() && RankSystem.HasPermission( x.SteamId, Permission.StaffChat ) )
						.Select( x => x.Connection )
						.ToHashSet();

					using ( Rpc.FilterInclude( c => staffPlayers.Contains( c ) ) )
					{
						BroadcastPlayerChat( messageId, callerId, message, MessageType.StaffChat );
					}

					break;
				}
			case MessageType.FactionChat:
				{
					if ( !player.FactionId.HasValue )
					{
						return;
					}

					var factionMembers = GameUtils.Players
						.Where( x => x.IsValid() && x.FactionId == player.FactionId )
						.Select( x => x.Connection )
						.ToHashSet();

					using ( Rpc.FilterInclude( c => factionMembers.Contains( c ) ) )
					{
						BroadcastPlayerChat( messageId, callerId, message, MessageType.FactionChat );
					}

					break;
				}
			case MessageType.Generic:
			case MessageType.GlobalChat:
			case MessageType.System:
			case MessageType.Minigame:
			default:
				BroadcastPlayerChat( messageId, callerId, message, MessageType.GlobalChat );
				break;
		}
	}

	private static HashSet<Connection> GetLocalChatRecipients( Player author )
	{
		var maxDist = Config.Current.Game.ChatMaxDistance;
		var authorPos = author.WorldPosition;
		var inRange = GameUtils.Players
			.Where( x => x.IsValid() &&
				Vector3.DistanceBetween( authorPos, x.GetListenerPosition() ) <= maxDist )
			.Select( x => x.Connection )
			.Where( x => x != null )
			.Cast<Connection>()
			.ToHashSet();

		// Always include the host so the Discord audit log inside BroadcastPlayerChat still fires.
		inRange.Add( Connection.Local );
		return inRange;
	}

	private static string BuildItemShareMessage( InventoryItemDto item )
	{
		return string.Join( ItemShareSeparator,
			item.Definition.Id,
			item.Quantity );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastPlayerChat( Guid messageId, Guid authorId, string message, MessageType messageType )
	{
		var author = GameUtils.GetPlayerByConnectionId( authorId );
		if ( !author.IsValid())
		{
			return;
		}
		
		// Log all chat messages to Discord, regardless of type, for moderation purposes
		if ( Networking.IsHost )
		{
			_ = ServerApiClient.Audit( "Chat", $"[{messageType}] {author.SteamName} ({author.SteamId}): {message}", author.SteamId );
		}

		if ( !Player.Local.IsValid() )
		{
			return;
		}

		if ( messageType == MessageType.LocalChat )
		{
			DoTts( message, author );
		}

		HandleChatSound( messageType, authorId );

		string? role = null;
		Color? roleColor = null;

		var playerRank = RankSystem.Instance.GetPlayerRank( author.SteamId );
		if ( playerRank != null && playerRank.Flags.HasFlag( RankFlags.ShowInChat ) )
		{
			role = playerRank.Name;
			roleColor = Color.FromRgb( playerRank.Color );
		}

		var entry = new ChatEntry( messageId, author.SteamId, author.DisplayName, message, 0.0f, messageType,
			Color.FromRgb( author.Job.Color ), role, roleColor );

		AddEntry( entry );
		
		Log.Info( $"[{messageType}] {author.DisplayName}: {message}" );
	}

	private void HandleChatSound( MessageType type, Guid authorId )
	{
		// Play sound for government chat
		if ( type == MessageType.GovernmentChat && !Cooldown.Current.CheckAndStartCooldown( "chat:government:sound", Config.Current.Game.ChatSoundNotifyCooldown ) )
		{
			Sound.Play( "radio" );
		}

		// Play sound for staff chat
		if ( type == MessageType.StaffChat && !Cooldown.Current.CheckAndStartCooldown( "chat:staff:sound", Config.Current.Game.ChatSoundNotifyCooldown ) )
		{
			Sound.Play( "radio" );
		}

		if ( type == MessageType.PrivateMessage && !Cooldown.Current.CheckAndStartCooldown( $"chat:msg:{authorId}:sound", Config.Current.Game.ChatSoundNotifyCooldown ) )
		{
			Sound.Play( "pm-notify" );
		}
	}

	public void BroadcastSystemText( string message )
	{
		Assert.True( Networking.IsHost );

		BroadcastChat( message, MessageType.System, Color.FromRgb( 7434470 ) );
	}

	/// <summary>
	/// Broadcasts a chat, intended for system messages or announcements or local responses.
	/// </summary>
	/// <param name="message">The content to send</param>
	/// <param name="type">Message type</param>
	/// <param name="color">Color of text</param>
	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastChat( string message, MessageType type = MessageType.Generic, Color? color = null )
	{
		if ( string.IsNullOrWhiteSpace( message ) )
		{
			return;
		}

		var entry = new ChatEntry( Guid.NewGuid(), 0, string.Empty, message, 0.0f, type,
			color ?? Color.White, null, null );

		AddEntry( entry );
		
		if ( Networking.IsHost )
		{
			_ = ServerApiClient.Audit( "Chat", $"[{type}] {message}", null );
		}
	}
	
	/// <summary>
	/// Sends a message that only the local player can see, intended for local responses to player actions or commands.
	/// </summary>
	/// <param name="message">What to send</param>
	public void Echo( string message )
	{
		if ( string.IsNullOrWhiteSpace( message ) )
		{
			return;
		}

		var entry = new ChatEntry( Guid.NewGuid(), 0, string.Empty, message, 0.0f, MessageType.Generic,
			Color.White, null, null );

		AddEntry( entry );
	}
}
