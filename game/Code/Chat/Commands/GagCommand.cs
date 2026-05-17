using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class GagCommand : ICommand
{
	public string Command => "gag";
	public string Help => Language.GetPhrase( "command.gag.help" );
	public bool IsUsableWhileDead => true;
	public Permission[] RequiredPermissions => [Permission.PlayerGag];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( args.Length < 3 )
		{
			caller.SendMessage( Language.GetPhrase( "command.gag.usage" ) );
			caller.SendMessage( Language.GetPhrase( "command.gag.duration_examples" ) );
			return true;
		}

		var targetIdentifier = args[0];
		var durationStr = args[1];
		var reason = string.Join( " ", args.Skip( 2 ) );

		var duration = CommandHelper.ParseDuration( durationStr );
		if ( duration == null )
		{
			caller.SendMessage( Language.GetPhrase( "command.gag.invalid_duration" ) );
			return true;
		}

		var targetPlayer = CommandHelper.ResolvePlayer( caller, targetIdentifier );
		if ( !targetPlayer.IsValid() )
			return true;

		_ = ServerApiClient.SanctionPlayer( targetPlayer.SteamId, new CreateSanctionDto
		{
			Reason = reason,
			Notes = $"Gagged by {caller.SteamName} ({caller.SteamId}) via chat command for {durationStr}.",
			Type = SanctionType.Gag,
			Duration = duration.Value
		} );

		caller.Success( string.Format( Language.GetPhrase( "command.gag.success" ), targetPlayer.DisplayName, durationStr, reason ) );

		Log.Info( $"[COMMAND] {caller.DisplayName} ({caller.SteamId}) gagged {targetPlayer.DisplayName} ({targetPlayer.SteamId}) for {durationStr}: {reason}" );
		_ = ServerApiClient.Audit( "Gag", $"{caller.SteamName} ({caller.SteamId}) gagged {targetPlayer.SteamName} ({targetPlayer.SteamId}) for {durationStr}: {reason}", caller.SteamId );

		return true;
	}
}
