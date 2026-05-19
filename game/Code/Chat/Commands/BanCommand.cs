using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class BanCommand : ICommand
{
	public string Command => "ban";
	public string Help => Language.GetPhrase( "command.ban.help" );
	public bool IsUsableWhileDead => true;
	public Permission[] RequiredPermissions => [Permission.PlayerBan];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( args.Length < 3 )
		{
			caller.SendMessage( Language.GetPhrase( "command.ban.usage" ) );
			caller.SendMessage( Language.GetPhrase( "command.ban.duration_examples" ) );
			return true;
		}

		var targetIdentifier = args[0];
		var durationStr = args[1];
		var reason = string.Join( " ", args.Skip( 2 ) );

		// Parse duration
		var permanent = IsPermanentDuration( durationStr );
		var duration = CommandHelper.ParseDuration( durationStr );
		if ( duration == null && !permanent )
		{
			caller.SendMessage( Language.GetPhrase( "command.ban.invalid_duration" ) );
			return true;
		}

		var targetPlayer = CommandHelper.ResolvePlayer( caller, targetIdentifier );
		if ( !targetPlayer.IsValid() )
			return true;

		if ( !RankSystem.CanTarget( caller.SteamId, targetPlayer.SteamId ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.sanction.cannot_target_higher_rank" ) );
			return true;
		}
		var durationDisplay = permanent
			? Language.GetPhrase( "command.ban.duration_permanent" )
			: string.Format( Language.GetPhrase( "command.ban.duration_temporary" ), durationStr );

		_ = ServerApiClient.SanctionPlayer( targetPlayer.SteamId, new CreateSanctionDto
		{
			Reason = reason,
			Notes = $"Banned by {caller.SteamName} ({caller.SteamId}) via chat command.",
			Type = SanctionType.Ban,
			Duration = duration
		} );

		GameNetworkManager.Instance.KickPlayer( targetPlayer.Connection, reason, isBan: true );

		caller.Success( string.Format( Language.GetPhrase( "command.ban.success" ), targetPlayer.DisplayName, durationDisplay, reason ) );

		Log.Info( $"[COMMAND] {caller.DisplayName} ({caller.SteamId}) banned {targetPlayer.DisplayName} ({targetPlayer.SteamId}) {durationDisplay}: {reason}" );
		_ = ServerApiClient.Audit( "Ban", $"{caller.SteamName} ({caller.SteamId}) banned {targetPlayer.SteamName} ({targetPlayer.SteamId}) {durationDisplay}: {reason}", caller.SteamId );

		return true;
	}

	private static bool IsPermanentDuration( string input )
	{
		return input.Equals( "permanent", StringComparison.OrdinalIgnoreCase )
		       || input.Equals( "perm", StringComparison.OrdinalIgnoreCase );
	}
}
