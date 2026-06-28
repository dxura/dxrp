namespace Dxura.RP.Game;

public enum GovernmentRestrainDenial
{
	None,
	NotGovernment,
	InvalidTarget,
	PoliticalPrisoner,
	MayorOnlyPolice,
	GovernmentTarget
}

public enum GovernmentRestrainAction
{
	Taser,
	Arrest
}

public partial class Governance : IGameEvents
{
	public static GovernmentRestrainDenial GetGovernmentRestrainDenial( Player actor, Player? target, GovernmentRestrainAction action )
	{
		if ( !target.IsValid() || target == actor )
		{
			return GovernmentRestrainDenial.InvalidTarget;
		}

		if ( !actor.Job.IsGovernmentRole() )
		{
			return GovernmentRestrainDenial.NotGovernment;
		}

		if ( target.Job.IsPoliticalPrisonerRole() && action == GovernmentRestrainAction.Arrest )
		{
			return GovernmentRestrainDenial.PoliticalPrisoner;
		}

		if ( actor.Job.IsMayoralRole() )
		{
			return action switch
			{
				// Mayor taser: civilians only.
				GovernmentRestrainAction.Taser when target.Job.IsGovernmentRole() => GovernmentRestrainDenial.GovernmentTarget,
				// Mayor handcuffs: police demote only.
				GovernmentRestrainAction.Arrest when !target.Job.IsPoliceRole() => GovernmentRestrainDenial.MayorOnlyPolice,
				_ => GovernmentRestrainDenial.None
			};
		}

		if ( target.Job.IsGovernmentRole() )
		{
			return GovernmentRestrainDenial.GovernmentTarget;
		}

		return GovernmentRestrainDenial.None;
	}

	public static bool IsMayorPoliceDemote( Player actor, Player target )
	{
		return actor.Job.IsMayoralRole() && target.Job.IsPoliceRole();
	}

	public bool CanGovernmentRestrainTarget( Player actor, Player target, GovernmentRestrainAction action )
	{
		var denial = GetGovernmentRestrainDenial( actor, target, action );
		if ( denial == GovernmentRestrainDenial.None )
		{
			return true;
		}

		NotifyGovernmentRestrainDenial( actor, denial, action );
		return false;
	}

	private static void NotifyGovernmentRestrainDenial( Player actor, GovernmentRestrainDenial denial, GovernmentRestrainAction action )
	{
		if ( denial is GovernmentRestrainDenial.None or GovernmentRestrainDenial.NotGovernment or GovernmentRestrainDenial.InvalidTarget )
		{
			return;
		}

		var taserContext = action == GovernmentRestrainAction.Taser;
		var errorKey = taserContext
			? denial switch
			{
				GovernmentRestrainDenial.GovernmentTarget => "#equipment.taser.cannot_target.government",
				_ => null
			}
			: denial switch
			{
				GovernmentRestrainDenial.PoliticalPrisoner => "#governance.jail.political.cannot_arrest",
				GovernmentRestrainDenial.MayorOnlyPolice => "#governance.jail.mayor.only_police",
				GovernmentRestrainDenial.GovernmentTarget => "#governance.jail.government.cannot_arrest",
				_ => null
			};

		if ( errorKey != null )
		{
			actor.Error( errorKey );
		}
	}
}
