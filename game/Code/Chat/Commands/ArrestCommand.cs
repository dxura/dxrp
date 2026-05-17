using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class ArrestCommand : ICommand
{
	public string Command => "arrest";
	public string Help => "/arrest <username/steamid> [duration] - Arrest a player";
	public bool IsUsableWhileDead => true;
	public Permission[] RequiredPermissions => [Permission.CommandArrest];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( args.Length == 0 )
		{
			caller.SendMessage( "Usage: /arrest <username/steamid> [duration]" );
			caller.SendMessage( "Duration examples: 10m, 1h, 1d" );
			return true;
		}

		var targetPlayer = CommandHelper.ResolvePlayer( caller, args[0] );
		if ( !targetPlayer.IsValid() )
		{
			return true;
		}

		if ( !RankSystem.CanTarget( caller.SteamId, targetPlayer.SteamId ) )
		{
			caller.SendMessage( "You cannot arrest a player with a higher rank." );
			return true;
		}

		var duration = TimeSpan.FromSeconds( Config.Current.Game.JailTime );
		var durationLabel = $"{Config.Current.Game.JailTime:0} seconds";

		if ( args.Length > 1 )
		{
			var parsedDuration = CommandHelper.ParseDuration( args[1] );
			if ( parsedDuration == null )
			{
				caller.SendMessage( "Invalid duration. Examples: 10m, 1h, 1d" );
				return true;
			}

			duration = parsedDuration.Value;
			durationLabel = args[1];
		}

		if ( targetPlayer.Job.IsPoliticalPrisonerRole() )
		{
			caller.SendMessage( "You cannot arrest political prisoners." );
			return true;
		}

		targetPlayer.SetSit( null );
		Governance.Current.Arrest( targetPlayer.SteamId, (float)duration.TotalSeconds );

		caller.Success( $"Arrested {targetPlayer.DisplayName} for {durationLabel}." );
		targetPlayer.Warn( $"You have been arrested by {caller.DisplayName}." );

		Log.Info( $"[COMMAND] {caller.DisplayName} ({caller.SteamId}) arrested {targetPlayer.DisplayName} ({targetPlayer.SteamId}) for {durationLabel}." );
		_ = ServerApiClient.Audit( "Arrest", $"{caller.SteamName} ({caller.SteamId}) arrested {targetPlayer.SteamName} ({targetPlayer.SteamId}) for {durationLabel} via staff command.", caller.SteamId );

		return true;
	}
}
