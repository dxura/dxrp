using Dxura.RP.Game.Minigame;
using Dxura.RP.Game.Minigame.Minigames;
using Dxura.RP.Shared;
namespace Dxura.RP.Game.Commands;

public class MinigameCommand : ICommand
{
	public string Command => "minigame";
	public string[] Aliases => ["mg"];
	public string Help => Language.GetPhrase( "command.minigame.help" );
	public bool IsUsableWhileDead => false;
	public bool IsUsableWhileRestricted => true;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( !MinigameSystem.Instance.IsValid() || !Config.Current.Game.MinigamesEnabled )
		{
			caller.SendMessage( Language.GetPhrase( "command.minigame.unavailable" ) );
			return true;
		}

		var subcommand = args.Length > 0 ? args[0].ToLower() : "toggle";
		var options = args.Length > 1 ? args[1..] : [];

		switch ( subcommand )
		{
			case "toggle":
				if ( MinigameSystem.Instance.IsPlayerInMinigame( caller ) )
				{
					LeaveMinigame( caller );
				}
				else
				{
					JoinMinigame( caller );
				}
				break;
			case "join":
				JoinMinigame( caller );
				break;
			case "leave":
				LeaveMinigame( caller );
				break;
			case "start":
				StartMinigame( caller, options );
				break;
			case "stop":
				StopMinigame( caller );
				break;
			case "skip":
				SkipMinigameStage( caller );
				break;
			case "list":
				ListMinigames( caller );
				break;
			default:
				caller.SendMessage( Language.GetPhrase( "command.minigame.unknown_subcommand" ) );
				break;
		}

		return true;
	}

	private void JoinMinigame( Player caller )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandMinigameParticipate ) )
		{
			caller.SendMessage( "#generic.permission" );
			return;
		}
		
		if ( !MinigameSystem.Instance.IsMinigameActive() )
		{
			caller.SendMessage( Language.GetPhrase( "command.minigame.no_active_join" ) );
			return;
		}
		
		if ( MinigameSystem.Instance.IsPlayerInMinigame( caller ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.minigame.already_joined" ) );
			return;
		}

		var didJoin = MinigameSystem.Instance.AddPlayerToMinigame( caller );

		caller.SendMessage( didJoin ? Language.GetPhrase( "command.minigame.joined" ) : Language.GetPhrase( "command.minigame.unable_join" ) );
	}

	private void LeaveMinigame( Player caller )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandMinigameParticipate ) )
		{
			caller.SendMessage( "#generic.permission" );
			return;
		}
		
		if ( !MinigameSystem.Instance.IsMinigameActive() )
		{
			caller.SendMessage( Language.GetPhrase( "command.minigame.no_active_leave" ) );
			return;
		}

		if ( !MinigameSystem.Instance.IsPlayerInMinigame( caller ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.minigame.not_in" ) );
			return;
		}

		if ( MinigameSystem.Instance.CurrentState == MinigameState.PreLobby )
		{
			caller.SendMessage( Language.GetPhrase( "command.minigame.cannot_leave_starting" ) );
			return;
		}

		MinigameSystem.Instance.RemovePlayerFromMinigame( caller );
		caller.SendMessage( Language.GetPhrase( "command.minigame.left" ) );
	}

	private void StartMinigame( Player caller, string[] options )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandMinigameManage ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.minigame.no_permission" ) );
			return;
		}

		if ( MinigameSystem.Instance.IsMinigameActive() )
		{
			caller.SendMessage( Language.GetPhrase( "command.minigame.already_active" ) );
			return;
		}

		MinigameResource? desiredMinigame = null;
		var specifiedMinigame = options.Length > 0 ? options[0].ToLower() : null;
		if ( !string.IsNullOrEmpty( specifiedMinigame ) )
		{
			desiredMinigame = MinigameResource.All.FirstOrDefault( x => x.Identifier.Contains( specifiedMinigame, StringComparison.CurrentCultureIgnoreCase ) );
			if ( desiredMinigame == null )
			{
				caller.SendMessage( string.Format( Language.GetPhrase( "command.minigame.not_found" ), specifiedMinigame ) );
				return;
			}
		}

		var secondaryHint = options.Length > 1 ? options[1].ToLower() : null;
		MinigameSystem.Instance.StartMinigame( desiredMinigame, secondaryHint );
		MinigameSystem.Instance.AddPlayerToMinigame( caller );
	}

	private void StopMinigame( Player caller )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandMinigameManage ) )
		{
			caller.SendMessage( "#generic.permission" );
			return;
		}

		if ( !MinigameSystem.Instance.IsMinigameActive() )
		{
			caller.SendMessage( Language.GetPhrase( "command.minigame.no_active" ) );
			return;
		}

		MinigameSystem.Instance.StopMinigame();
		caller.SendMessage( Language.GetPhrase( "command.minigame.stopped" ) );
	}

	private void SkipMinigameStage( Player caller )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandMinigameManage ) )
		{
			caller.SendMessage( "#generic.permission" );
			return;
		}

		if ( !MinigameSystem.Instance.IsMinigameActive() )
		{
			caller.SendMessage( Language.GetPhrase( "command.minigame.no_active" ) );
			return;
		}

		MinigameSystem.Instance.SkipMinigameStage();
		caller.SendMessage( Language.GetPhrase( "command.minigame.skipped" ) );
	}

	private void ListMinigames( Player caller )
	{
		var minigames = MinigameResource.All.ToList();

		if ( minigames.Count == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.minigame.none_available" ) );
			return;
		}

		caller.SendMessage( string.Format( Language.GetPhrase( "command.minigame.list" ), string.Join( ", ", minigames.Select( m => m.Identifier ) ) ) );
	}
}
