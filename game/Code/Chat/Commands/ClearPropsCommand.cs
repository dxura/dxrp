using Dxura.RP.Shared;
namespace Dxura.RP.Game.Commands;

public class ClearPropsCommand : ICommand
{
	public string Command => "clearprops";
	public string Help => "/clearprops - Clear your props, or a player's props (if permitted)";

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		// Staff+ can clear another player's props
		if ( args.Length > 0 )
		{
			if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandClearProps ) )
			{
				caller.SendMessage( "#generic.permission" );
				return true;
			}

			var targetName = string.Join( " ", args );
			var matchingPlayers = GameUtils.GetPlayersByName( targetName );

			if ( matchingPlayers.Count == 0 )
			{
				caller.Error( string.Format( Language.GetPhrase( "command.clearprops.not_found" ), targetName ) );
				return true;
			}

			if ( matchingPlayers.Count > 1 )
			{
				var playerNames = string.Join( ", ", matchingPlayers.Select( p => p.DisplayName ) );
				caller.SendMessage( string.Format( Language.GetPhrase( "command.clearprops.multiple" ), targetName, playerNames ) );
				return true;
			}

			var target = matchingPlayers[0];
			CleanupSystem.Current.CleanupConstructs( target.SteamId);
			caller.Success( string.Format( Language.GetPhrase( "command.clearprops.cleared_for" ), target.DisplayName ) );
			Log.Info( $"Staff {caller.DisplayName} cleared props for {target.DisplayName}" );

			return true;
		}

		// Self-clear with cooldown
		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:clearprops", Config.Current.Game.UtilityClearCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		CleanupSystem.Current.CleanupConstructs( caller.SteamId, ConstructType.Prop );
		caller.Success( Language.GetPhrase( "command.clearprops.cleared" ) );
		Log.Info( $"Player {caller.DisplayName} cleared their props" );

		return true;
	}
}
