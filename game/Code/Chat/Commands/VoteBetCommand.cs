using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class VoteBetCommand : ICommand
{
	public const string Name = "votebet";
	public string Command => Name;
	public string[] Aliases => new[]
	{
		"vb"
	};
	public string Help => Language.GetPhrase( "command.votebet.help" );

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( !Config.Current.Game.MoneyEnabled )
		{
			caller.SendMessage( Language.GetPhrase( "command.votebet.disabled" ) );
			return true;
		}

		if ( args.Length < 1 )
		{
			return false;
		}

		var subcommand = args[0].ToLower();

		switch ( subcommand )
		{
			case "start":
				return HandleStart( caller, args );
			case "join":
				return HandleJoin( caller, args );
			case "lock":
				return HandleLock( caller );
			case "end":
				return HandleEnd( caller, args );
			default:
				return false;
		}
	}

	private bool HandleStart( Player caller, string[] args )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandVoteBetManage ) )
		{
			caller.SendMessage( "#generic.permission" );
			return true;
		}

		// Parse: /vb start Description here | Option1 | Option2
		var text = string.Join( " ", args.Skip( 1 ) );
		var segments = text.Split( '|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );

		if ( segments.Length < 3 )
		{
			caller.SendMessage( Language.GetPhrase( "command.votebet.usage_start" ) );
			return true;
		}

		var description = segments[0];
		var outcomes = segments.Skip( 1 ).ToList();

		VoteBetSystem.Instance?.StartVoteBet( caller, description, outcomes );
		return true;
	}

	private bool HandleJoin( Player caller, string[] args )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandVoteBetParticipate ) )
		{
			caller.SendMessage( "#generic.permission" );
			return true;
		}
		
		var activeVoteBet = VoteBetSystem.Instance?.GetActiveVoteBet();
		if ( !activeVoteBet.HasValue )
		{
			caller.SendMessage( Language.GetPhrase( "command.votebet.no_active_join" ) );
			return true;
		}

		if ( activeVoteBet.Value.IsLocked )
		{
			caller.SendMessage( Language.GetPhrase( "command.votebet.locked" ) );
			return true;
		}

		if ( VoteBetSystem.Instance?.HasPlayerBet( caller.SteamId, activeVoteBet.Value.Id ) ?? false )
		{
			caller.SendMessage( Language.GetPhrase( "command.votebet.already_bet" ) );
			return true;
		}

		if ( args.Length < 3 || !int.TryParse( args[1], out var outcomeNum ) || !uint.TryParse( args[2], out var betAmount ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.votebet.usage_join" ) );
			return true;
		}

		if ( outcomeNum < 1 || outcomeNum > activeVoteBet.Value.Outcomes.Count )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.votebet.invalid_outcome" ), activeVoteBet.Value.Outcomes.Count ) );
			return true;
		}

		if ( betAmount == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.votebet.bet_zero" ) );
			return true;
		}

		VoteBetSystem.Instance?.JoinVoteBet( caller, activeVoteBet.Value.Id, outcomeNum - 1, betAmount );
		return true;
	}

	private bool HandleLock( Player caller )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandVoteBetManage ) )
		{
			caller.SendMessage( "#generic.permission" );
			return true;
		}

		var activeVoteBet = VoteBetSystem.Instance?.GetActiveVoteBet();
		if ( !activeVoteBet.HasValue )
		{
			caller.SendMessage( Language.GetPhrase( "command.votebet.no_active_lock" ) );
			return true;
		}

		VoteBetSystem.Instance?.LockVoteBet( caller, activeVoteBet.Value.Id );
		return true;
	}

	private bool HandleEnd( Player caller, string[] args )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandVoteBetManage ) )
		{
			caller.SendMessage( "#generic.permission" );
			return true;
		}

		var activeVoteBet = VoteBetSystem.Instance?.GetActiveVoteBet();
		if ( !activeVoteBet.HasValue )
		{
			caller.SendMessage( Language.GetPhrase( "command.votebet.no_active_end" ) );
			return true;
		}

		if ( args.Length < 2 || !int.TryParse( args[1], out var outcomeNum ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.votebet.usage_end" ) );
			return true;
		}

		if ( outcomeNum < 1 || outcomeNum > activeVoteBet.Value.Outcomes.Count )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.votebet.invalid_outcome" ), activeVoteBet.Value.Outcomes.Count ) );
			return true;
		}

		VoteBetSystem.Instance?.EndVoteBet( caller, activeVoteBet.Value.Id, outcomeNum - 1 );
		return true;
	}
}
