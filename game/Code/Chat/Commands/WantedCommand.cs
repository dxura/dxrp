namespace Dxura.RP.Game.Commands;

public class WantedCommand : ICommand
{
	public string Command => "wanted";
	public string Help => Language.GetPhrase( "command.wanted.help" );
	public bool IsUsableWhileDead => false;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		var cooldownId = $"{caller.SteamId}:wanted";
		if ( Cooldown.Current.CheckAndStartCooldown( cooldownId, Config.Current.Game.WantedCooldown ) )
		{
			caller.Cooldown( cooldownId );
			return true;
		}

		if ( args.Length < 2 )
		{
			caller.Error( Language.GetPhrase( "command.wanted.usage" ) );
			return true;
		}

		var targetName = args[0];
		var reason = string.Join( " ", args.Skip( 1 ) );

		var matchingPlayers = GameUtils.GetPlayersByName( targetName );

		if ( matchingPlayers.Count == 0 )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.wanted.not_found" ), targetName ) );
			return true;
		}

		var target = matchingPlayers.First();

		if ( !target.IsValid() )
		{
			caller.Error( string.Format( Language.GetPhrase( "command.wanted.player_not_found" ), targetName ) );
			return true;
		}

		Governance.Current.WantedHost( caller, target, reason );
		return true;
	}
}
