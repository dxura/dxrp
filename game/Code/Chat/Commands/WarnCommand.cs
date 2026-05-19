using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class WarnCommand : ICommand
{
	public string Command => "warn";
	public string Help => Language.GetPhrase( "command.warn.help" );
	public bool IsUsableWhileDead => true;
	public Permission[] RequiredPermissions => [Permission.PlayerWarn];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( args.Length < 2 )
		{
			caller.SendMessage( Language.GetPhrase( "command.warn.usage" ) );
			return true;
		}

		var targetIdentifier = args[0];
		var reason = string.Join( " ", args.Skip( 1 ) );

		var targetPlayer = CommandHelper.ResolvePlayer( caller, targetIdentifier );
		if ( !targetPlayer.IsValid() )
			return true;

		if ( !RankSystem.CanTarget( caller.SteamId, targetPlayer.SteamId ) )
		{
			caller.SendMessage( "#command.errors.higher_rank" );
			return true;
		}

		_ = ServerApiClient.SanctionPlayer( targetPlayer.SteamId, new CreateSanctionDto
		{
			Reason = reason,
			Notes = $"Warned by {caller.SteamName} ({caller.SteamId}) via chat command.",
			Type = SanctionType.Warning
		} );

		caller.Success( string.Format( Language.GetPhrase( "command.warn.success" ), targetPlayer.DisplayName, reason ) );

		Log.Info( $"[COMMAND] {caller.DisplayName} ({caller.SteamId}) warned {targetPlayer.DisplayName} ({targetPlayer.SteamId}): {reason}" );
		_ = ServerApiClient.Audit( "Warn", $"{caller.SteamName} ({caller.SteamId}) warned {targetPlayer.SteamName} ({targetPlayer.SteamId}): {reason}", caller.SteamId );

		return true;
	}
}
