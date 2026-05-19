using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class CancelDemoteCommand : ICommand
{
	public string Command => "canceldemote";
	public string Help => Language.GetPhrase( "command.canceldemote.help" );
	public bool IsUsableWhileDead => true;
	public Permission[] RequiredPermissions => [Permission.CommandCancelDemote];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( args.Length == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.canceldemote.usage" ) );
			return true;
		}

		var targetPlayer = CommandHelper.ResolvePlayer( caller, string.Join( " ", args ) );
		if ( !targetPlayer.IsValid() )
			return true;

		if ( !RankSystem.CanTarget( caller.SteamId, targetPlayer.SteamId ) )
		{
			caller.SendMessage( "#command.errors.higher_rank" );
			return true;
		}

		if ( !VoteSystem.Instance.IsValid() )
		{
			caller.Error( "#generic.error" );
			return true;
		}

		var cancelled = VoteSystem.Instance.CancelDemoteVotesHost( targetPlayer.SteamId );

		if ( cancelled == 0 )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.canceldemote.none" ), targetPlayer.DisplayName ) );
			return true;
		}

		caller.Success( string.Format( Language.GetPhrase( "command.canceldemote.success" ), cancelled, targetPlayer.DisplayName ) );
		targetPlayer.Info( string.Format( Language.GetPhrase( "command.canceldemote.target" ), caller.DisplayName ) );

		Log.Info( $"[COMMAND] {caller.DisplayName} ({caller.SteamId}) cancelled {cancelled} demote vote(s) against {targetPlayer.DisplayName} ({targetPlayer.SteamId})" );
		_ = ServerApiClient.Audit( "CancelDemote", $"{caller.SteamName} ({caller.SteamId}) cancelled {cancelled} demote vote(s) against {targetPlayer.SteamName} ({targetPlayer.SteamId}).", caller.SteamId );

		return true;
	}
}
