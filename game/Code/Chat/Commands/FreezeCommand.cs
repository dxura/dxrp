using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class FreezeCommand : ICommand
{
	public const string Name = "freeze";

	public string Command => Name;
	public string Help => Language.GetPhrase( "command.freeze.help" );
	public bool IsUsableWhileDead => true;
	public float? CooldownOverride => 0f;
	public Permission[] RequiredPermissions => [Permission.CommandFreeze];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( args.Length == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.freeze.usage" ) );
			return true;
		}

		var targetPlayer = CommandHelper.ResolvePlayer( caller, string.Join( " ", args ) );
		if ( !targetPlayer.IsValid() )
		{
			return false;
		}

		if ( !RankSystem.CanTarget( caller.SteamId, targetPlayer.SteamId ) )
		{
			caller.SendMessage( "#command.errors.higher_rank" );
			return true;
		}

		if ( targetPlayer.HasStatus( Constants.FreezeStatus ) )
		{
			targetPlayer.RemoveStatus( Constants.FreezeStatus );
			caller.Success( string.Format( Language.GetPhrase( "command.freeze.unfroze" ), targetPlayer.DisplayName ) );
		}
		else
		{
			targetPlayer.AddStatus( Constants.FreezeStatus );
			caller.Success( string.Format( Language.GetPhrase( "command.freeze.froze" ), targetPlayer.DisplayName ) );
		}

		_ = ServerApiClient.Audit( "Freeze", $"{caller.SteamName} ({caller.SteamId}) toggled freeze on {targetPlayer.SteamName} ({targetPlayer.SteamId})", caller.SteamId );
		return true;
	}
}
