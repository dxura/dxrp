using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class ClearEntitiesCommand : ICommand
{
	public string Command => "clearentities";
	public string Help => Language.GetPhrase( "command.clearentities.help" );
	public bool IsUsableWhileDead => true;
	public float? CooldownOverride => 0f;
	public Permission[] RequiredPermissions => [Permission.CommandClearEntities];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( args.Length == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.clearentities.usage" ) );
			return true;
		}

		var targetPlayer = CommandHelper.ResolvePlayer( caller, string.Join( " ", args ) );
		if ( !targetPlayer.IsValid() )
		{
			return false;
		}

		CleanupSystem.Current.CleanupEntities( targetPlayer.SteamId, true );
		caller.Success( string.Format( Language.GetPhrase( "command.clearentities.success" ), targetPlayer.DisplayName ) );

		_ = ServerApiClient.Audit( "ClearEntities", $"{caller.SteamName} ({caller.SteamId}) cleared entities for {targetPlayer.SteamName} ({targetPlayer.SteamId})", caller.SteamId );
		return true;
	}
}
