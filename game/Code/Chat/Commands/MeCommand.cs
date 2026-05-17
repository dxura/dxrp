namespace Dxura.RP.Game.Commands;

public class MeCommand : ICommand
{
	public string Command => "me";
	public string Help => "Perform a RP action. Usage: /me <action>";

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:me", Config.Current.Game.MeCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		if ( args.Length == 0 )
		{
			return false;
		}

		var message = string.Join( " ", args );

		message = message.Truncate( Config.Current.Game.ChatMaxLength );

		message = GameManager.ModerateText( caller.SteamId, "ME", message );

		var formattedMessage = $"{caller.DisplayName} {message}";
		formattedMessage = formattedMessage.ToLowerInvariant();

		BroadcastAction( caller, formattedMessage );

		return true;
	}

	private void BroadcastAction( Player caller, string message )
	{
		// Get players within nearby chat range
		var nearbyPlayers = GameUtils.Players
			.Where( p => p.IsValid() &&
			             Vector3.DistanceBetween( caller.WorldPosition, p.WorldPosition ) <= Config.Current.Game.ChatMaxDistance )
			.Select( p => p.Connection )
			.ToHashSet();


		// Broadcast to local players only with white color and no TTS
		using ( Rpc.FilterInclude( c => nearbyPlayers.Contains( c ) ) )
		{
			Chat.Current.BroadcastChat( message, MessageType.Generic, Color.FromBytes( 255, 255, 255, 180 ) );
		}

		_ = ServerApiClient.Audit( "Me", $"Player {caller.SteamName} ({caller.SteamId}) used ME: {message}", caller.SteamId );
	}
}
