using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class JailCommand : ICommand
{
	public string Command => "jail";
	public string Help => Language.GetPhrase( "command.jail.help" );
	public bool IsUsableWhileDead => true;
	public Permission[] RequiredPermissions => [Permission.PlayerJail];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( args.Length < 3 )
		{
			caller.SendMessage( Language.GetPhrase( "command.jail.usage" ) );
			caller.SendMessage( Language.GetPhrase( "command.jail.duration_examples" ) );
			return true;
		}

		var targetIdentifier = args[0];
		var durationStr = args[1];
		var reason = string.Join( " ", args.Skip( 2 ) );

		var duration = CommandHelper.ParseDuration( durationStr );
		if ( duration == null )
		{
			caller.SendMessage( Language.GetPhrase( "command.jail.invalid_duration" ) );
			return true;
		}

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
			Notes = $"Jailed by {caller.SteamName} ({caller.SteamId}) via chat command for {durationStr}.",
			Type = SanctionType.Jail,
			Duration = duration.Value
		} );

		caller.Success( string.Format( Language.GetPhrase( "command.jail.success" ), targetPlayer.DisplayName, durationStr, reason ) );

		Log.Info( $"[COMMAND] {caller.DisplayName} ({caller.SteamId}) jailed {targetPlayer.DisplayName} ({targetPlayer.SteamId}) for {durationStr}: {reason}" );
		_ = ServerApiClient.Audit( "Arrest", $"{caller.SteamName} ({caller.SteamId}) jailed {targetPlayer.SteamName} ({targetPlayer.SteamId}) for {durationStr}: {reason}", caller.SteamId );

		return true;
	}
}
