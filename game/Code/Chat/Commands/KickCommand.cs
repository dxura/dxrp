using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class KickCommand : ICommand
{
	public string Command => "kick";
	public string Help => Language.GetPhrase( "command.kick.help" );
	public bool IsUsableWhileDead => true;
	public Permission[] RequiredPermissions => [Permission.PlayerKick];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( args.Length < 2 )
		{
			caller.SendMessage( Language.GetPhrase( "command.kick.usage" ) );
			return true;
		}

		var targetIdentifier = args[0];
		var reason = string.Join( " ", args.Skip( 1 ) );

		var targetPlayer = CommandHelper.ResolvePlayer( caller, targetIdentifier );
		if ( !targetPlayer.IsValid() )
			return true;

		if ( !RankSystem.CanTarget( caller.SteamId, targetPlayer.SteamId ) )
		{
			caller.SendMessage( "You cannot sanction a player with a higher rank." );
			return true;
		}

		_ = ServerApiClient.SanctionPlayer( targetPlayer.SteamId, new CreateSanctionDto
		{
			Reason = reason,
			Notes = $"Kicked by {caller.SteamName} ({caller.SteamId}) via chat command.",
			Type = SanctionType.Kick
		} );

		GameNetworkManager.Instance.KickPlayer( targetPlayer.Connection, reason );

		caller.Success( string.Format( Language.GetPhrase( "command.kick.success" ), targetPlayer.DisplayName, reason ) );

		Log.Info( $"[COMMAND] {caller.DisplayName} ({caller.SteamId}) kicked {targetPlayer.DisplayName} ({targetPlayer.SteamId}): {reason}" );
		_ = ServerApiClient.Audit( "Kick", $"{caller.SteamName} ({caller.SteamId}) kicked {targetPlayer.SteamName} ({targetPlayer.SteamId}): {reason}", caller.SteamId );

		return true;
	}
}
