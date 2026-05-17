using Dxura.RP.Shared;
namespace Dxura.RP.Game.Commands;

public class SpectateCommand : ICommand
{
	public const string Name = "spectate";

	public string Command => Name;
	public string[] Aliases => ["spec"];
	public string Help => "Spectate a player. Usage: /spectate <player> or /spectate to stop.";
	public bool IsUsableWhileDead => false;
	public Permission[] RequiredPermissions => [Permission.PlayerSpectate];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() || !caller.Controller.IsValid() )
		{
			return false;
		}

		var spectateMode = caller.Controller.Components.Get<MoveModeSpectate>();
		if ( !spectateMode.IsValid() )
		{
			caller.SendMessage( "Spectate mode is not available." );
			return true;
		}

		// No args = stop spectating
		if ( args.Length == 0 )
		{
			if ( spectateMode.SpectateTarget.IsValid() )
			{
				spectateMode.StopSpectatingHost();
				Log.Info( $"[COMMAND] {caller.SteamName} ({caller.SteamId}) stopped spectating" );
				_ = ServerApiClient.Audit( "Spectate", $"{caller.SteamName} ({caller.SteamId}) stopped spectating", caller.SteamId );
			}
			else
			{
				caller.SendMessage( "You are not spectating anyone. Usage: /spectate <player>" );
			}

			return true;
		}

		var targetIdentifier = string.Join( " ", args );
		var targetPlayer = CommandHelper.ResolvePlayer( caller, targetIdentifier );
		if ( !targetPlayer.IsValid() )
			return true;

		if ( targetPlayer == caller )
		{
			caller.SendMessage( "You cannot spectate yourself." );
			return true;
		}

		if ( !RankSystem.CanTarget( caller.SteamId, targetPlayer.SteamId ) )
		{
			caller.SendMessage( "You cannot spectate a player with a higher rank." );
			return true;
		}

		spectateMode.StartSpectating( targetPlayer.GameObject );
		caller.SendMessage( $"Now spectating {targetPlayer.DisplayName}." );

		Log.Info( $"[COMMAND] {caller.SteamName} ({caller.SteamId}) started spectating {targetPlayer.SteamName} ({targetPlayer.SteamId})" );
		_ = ServerApiClient.Audit( "Spectate", $"{caller.SteamName} ({caller.SteamId}) started spectating {targetPlayer.SteamName} ({targetPlayer.SteamId})", caller.SteamId );

		return true;
	}
}
