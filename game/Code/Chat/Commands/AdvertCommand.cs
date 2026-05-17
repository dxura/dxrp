namespace Dxura.RP.Game.Commands;

public class AdvertCommand : ICommand
{
	public string Command => "advert";
	public string Help => "/advert <message> - Sends an advertisement message to all players";

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}


		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:advert", Config.Current.Game.AdvertCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		if ( args.Length == 0 )
		{
			return false;
		}

		var message = string.Join( " ", args );
		if ( message.Length > 100 )
		{
			caller.Error( Language.GetPhrase( "command.advert.too_long" ) );
			return false;
		}

		message = GameManager.ModerateText( caller.SteamId, "ADVERT", message );

		Chat.Current.BroadcastChat( $"[{Language.GetPhrase( "command.advert.prefix" )}] {message}", MessageType.Generic, Color.Yellow );
		_ = ServerApiClient.Audit( "Advert", $"Player {caller.SteamName} ({caller.SteamId}) sent advert: {message}", caller.SteamId );

		return true;
	}
}
