using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class UnarrestCommand : ICommand
{
	public string Command => "unarrest";
	public string Help => "/unarrest [username/steamid] - Release yourself or another player from arrest";
	public bool IsUsableWhileDead => true;
	public bool IsUsableWhileRestricted => true;
	public Permission[] RequiredPermissions => [Permission.CommandUnarrest];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		var targetPlayer = args.Length == 0
			? caller
			: CommandHelper.ResolvePlayer( caller, string.Join( " ", args ) );

		if ( !targetPlayer.IsValid() )
		{
			return true;
		}

		if ( targetPlayer != caller && !RankSystem.CanTarget( caller.SteamId, targetPlayer.SteamId ) )
		{
			caller.SendMessage( "You cannot unarrest a player with a higher rank." );
			return true;
		}

		if ( !Governance.Current.Prisoners.ContainsKey( targetPlayer.SteamId ) )
		{
			caller.SendMessage( $"{targetPlayer.DisplayName} is not arrested." );
			return true;
		}

		Governance.Current.Release( targetPlayer.SteamId );

		if ( targetPlayer == caller )
		{
			caller.Success( "Released yourself from arrest." );
		}
		else
		{
			caller.Success( $"Released {targetPlayer.DisplayName} from arrest." );
			targetPlayer.Success( $"You have been released by {caller.DisplayName}." );
		}

		Log.Info( $"[COMMAND] {caller.DisplayName} ({caller.SteamId}) unarrested {targetPlayer.DisplayName} ({targetPlayer.SteamId})." );
		_ = ServerApiClient.Audit( "Unarrest", $"{caller.SteamName} ({caller.SteamId}) unarrested {targetPlayer.SteamName} ({targetPlayer.SteamId}) via staff command.", caller.SteamId );

		return true;
	}
}
