namespace Dxura.RP.Game.System.Events;

/// <summary>
/// Event that temporarily removes voting and playtime requirements for all jobs
/// </summary>
public class RecruitmentDriveEvent : BaseEvent
{
	public const string EventIdentifier = "recruitment_drive";

	public override string Identifier => EventIdentifier;
	public override string Name => "Recruitment Drive";
	public override string Description => "All jobs are available without voting or playtime requirements!";
	public override float Duration => 300f; // 5 minutes
	public override int Weight => 60;

	protected override void OnStart()
	{
		if ( GameManager.Instance.IsValid() )
		{
			GameManager.Instance.IgnoreJobRequirements = true;
		}
	}

	protected override void OnEnd()
	{
		if ( GameManager.Instance.IsValid() )
		{
			GameManager.Instance.IgnoreJobRequirements = false;
		}
	}

	public override bool CanTrigger()
	{
		// Only trigger if there are players and some jobs have restrictions
		return base.CanTrigger() &&
		       GameModeJobs.All.Any( job => job.VoteRequired || job.PlayTime.HasValue );
	}
}
