namespace Dxura.RP.Game.Commands;

public class WarrantCommand : ICommand
{
	public string Command => "warrant";
	public string Help => Language.GetPhrase( "command.warrant.help" );
	public bool IsUsableWhileDead => false;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		var isGovernment = caller.Job.IsGovernmentRole();
		var isMayor = caller.Job.IsMayoralRole();
		var isChief = caller.Job.IsChiefRole();
		var canApprove = isMayor || isChief;

		if ( !isGovernment )
		{
			caller.SendMessage( "#generic.permission" );
			return true;
		}

		// Police (government except Mayor) can request with <player> <reason>
		if ( args.Length >= 2 )
		{
			if ( isMayor )
			{
				caller.SendMessage( Language.GetPhrase( "command.warrant.mayor_cannot" ) );
				return true;
			}

			var targetName = args[0];
			var reason = string.Join( " ", args.Skip( 1 ) );

			var matchingPlayers = GameUtils.GetPlayersByName( targetName );

			if ( matchingPlayers.Count == 0 )
			{
				caller.SendMessage( string.Format( Language.GetPhrase( "command.warrant.not_found" ), targetName ) );
				return true;
			}

			var target = matchingPlayers.First();

			if ( !target.IsValid() )
			{
				caller.SendMessage( string.Format( Language.GetPhrase( "command.warrant.player_not_found" ), targetName ) );
				return true;
			}

			Governance.Current.WarrantHost( caller, target, Governance.WarrantAction.Request, reason );
			return true;
		}

		// Mayor or Chief can approve/deny
		if ( args.Length == 1 && canApprove )
		{
			var action = args[0].ToLower();

			if ( action != "approve" && action != "deny" )
			{
				caller.SendMessage( Language.GetPhrase( "command.warrant.action_invalid" ) );
				return true;
			}

			var warrantAction = action == "approve"
				? Governance.WarrantAction.Approve
				: Governance.WarrantAction.Deny;

			var latestWarrant = Governance.Current.PendingWarrants
				.OrderByDescending( kvp => kvp.Value.ExpiresAt )
				.FirstOrDefault();

			if ( latestWarrant.Value == null )
			{
				caller.SendMessage( Language.GetPhrase( "command.warrant.no_pending" ) );
				return true;
			}

			var target = GameUtils.GetPlayerById( latestWarrant.Key );

			if ( !target.IsValid() )
			{
				caller.SendMessage( Language.GetPhrase( "command.warrant.target_offline" ) );
				Governance.Current.PendingWarrants.Remove( latestWarrant.Key );
				return true;
			}

			Governance.Current.WarrantHost( caller, target, warrantAction, string.Empty );
			return true;
		}

		// Invalid usage
		if ( canApprove )
		{
			caller.SendMessage( Language.GetPhrase( "command.warrant.usage_approver" ) );
		}
		else
		{
			caller.SendMessage( Language.GetPhrase( "command.warrant.usage" ) );
		}
		return true;
	}
}
