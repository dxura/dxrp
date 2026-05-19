using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class JobCommand : ICommand
{
	public const string Name = "job";

	public string Command => Name;
	public string Help => Language.GetPhrase( "command.job.help" );

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() || !Config.Current.Game.JobsEnabled )
		{
			return false;
		}

		if ( args.Length == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.job.usage" ) );
			return true;
		}

		if ( args.Length == 1 && args[0].Equals( "list", StringComparison.OrdinalIgnoreCase ) )
		{
			SendJobList( caller );
			return true;
		}

		if ( args.Length >= 2 )
		{
			ForceAssignJob( caller, args[0], string.Join( ' ', args.Skip( 1 ) ) );
			return true;
		}

		RequestOwnJob( caller, args[0] );
		return true;
	}

	private static void SendJobList( Player caller )
	{
		var jobs = GameModeJobs.All
			.Where( job => job.Selectable )
			.OrderBy( job => job.DisplayName() )
			.Select( job => job.VoteRequired
				? $"{job.DisplayName()}*"
				: job.DisplayName() );

		caller.SendMessage( string.Format( Language.GetPhrase( "command.job.list" ), string.Join( ", ", jobs ) ) );
	}

	private static void RequestOwnJob( Player caller, string jobInput )
	{
		var job = ResolveJob( jobInput, selectableOnly: true );
		if ( job == null )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.job.invalid" ), jobInput ) );
			return;
		}

		if ( caller.Job == job )
		{
			return;
		}

		if ( !job.AssignableTo( caller ) )
		{
			return;
		}

		var ignoreJobRequirements = GameManager.Instance.IsValid() && GameManager.Instance.IgnoreJobRequirements;
		if ( job.VoteRequired && !ignoreJobRequirements )
		{
			var jobCooldownId = $"{caller.SteamId}:job:vote:{job.Id}";
			if ( Cooldown.Current.IsOnCooldown( jobCooldownId ) )
			{
				var remainingTime = Cooldown.Current.GetRemainingTime( jobCooldownId );
				caller.Warn( string.Format(
					Language.GetPhrase( "notify.vote.cooldown" ),
					job.DisplayName(),
					remainingTime ) );
				return;
			}

			var voteCooldownId = $"{caller.SteamId}:vote";
			if ( Cooldown.Current.IsOnCooldown( voteCooldownId ) )
			{
				caller.Cooldown( voteCooldownId );
				return;
			}

			if ( !VoteSystem.Instance.IsValid() )
			{
				caller.Error( "#generic.error" );
				return;
			}

			if ( VoteSystem.Instance.StartVoteForPlayerHost( caller, caller.SteamId, VoteType.Job, customData: job.Id.ToString() ) )
			{
				caller.Success( string.Format(
					Language.GetPhrase( "command.job.vote_started" ),
					job.DisplayName() ) );
			}

			return;
		}

		var cooldownId = $"{caller.SteamId}:job:change";
		if ( Cooldown.Current.CheckAndStartCooldown( cooldownId, Config.Current.Game.JobChangeCooldown ) )
		{
			caller.Cooldown( cooldownId );
			return;
		}

		caller.AssignJobHost( job );
		caller.UnlockAchievement( "change_jobs" );
		caller.Success( string.Format(
			Language.GetPhrase( "command.job.changed" ),
			job.DisplayName() ) );

		Log.Info( $"[COMMAND] {caller.DisplayName} ({caller.SteamId}) changed job to {job.DisplayName()} [{job.Id}]" );
		_ = ServerApiClient.Audit( "Job", $"{caller.SteamName} ({caller.SteamId}) changed job to {job.DisplayName()} [{job.Id}]", caller.SteamId );
	}

	private static void ForceAssignJob( Player caller, string targetIdentifier, string jobInput )
	{
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.CommandJobManage ) )
		{
			caller.SendMessage( "#generic.permission" );
			return;
		}

		var targetPlayer = CommandHelper.ResolvePlayer( caller, targetIdentifier );
		if ( !targetPlayer.IsValid() )
		{
			return;
		}

		if ( !RankSystem.CanTarget( caller.SteamId, targetPlayer.SteamId ) )
		{
			caller.SendMessage( "#command.errors.higher_rank" );
			return;
		}

		var job = ResolveJob( jobInput, selectableOnly: false );
		if ( job == null )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.job.invalid" ), jobInput ) );
			return;
		}

		if ( targetPlayer.Job == job )
		{
			caller.SendMessage( string.Format(
				Language.GetPhrase( "command.job.already" ),
				targetPlayer.DisplayName,
				job.DisplayName() ) );
			return;
		}

		targetPlayer.AssignJobForcedHost( job );

		var jobName = job.DisplayName();
		caller.Success( string.Format( Language.GetPhrase( "command.job.force_success" ), targetPlayer.DisplayName, jobName ) );
		targetPlayer.Info( string.Format( Language.GetPhrase( "command.job.force_target" ), jobName, caller.DisplayName ) );

		Log.Info( $"[COMMAND] {caller.DisplayName} ({caller.SteamId}) force-set {targetPlayer.DisplayName} ({targetPlayer.SteamId}) to {job.DisplayName()} [{job.Id}]" );
		_ = ServerApiClient.Audit( "JobForce", $"{caller.SteamName} ({caller.SteamId}) force-set {targetPlayer.SteamName} ({targetPlayer.SteamId}) to {job.DisplayName()} [{job.Id}]", caller.SteamId );
	}

	private static GameModeJobDto? ResolveJob( string input, bool selectableOnly )
	{
		if ( string.IsNullOrWhiteSpace( input ) )
		{
			return null;
		}

		if ( Guid.TryParse( input, out var jobId ) )
		{
			return GameModeJobs.All
				.Where( job => !selectableOnly || job.Selectable )
				.FirstOrDefault( job => job.Id == jobId );
		}

		var normalizedInput = NormalizeJobName( input );
		return GameModeJobs.All
			.Where( job => !selectableOnly || job.Selectable )
			.FirstOrDefault( job => NormalizeJobName( job.DisplayName() ) == normalizedInput || NormalizeJobName( job.Name ) == normalizedInput );
	}

	private static string NormalizeJobName( string value )
	{
		return new string( value
			.Where( c => c != ' ' && c != '_' && c != '-' )
			.Select( char.ToLowerInvariant )
			.ToArray() );
	}
}
