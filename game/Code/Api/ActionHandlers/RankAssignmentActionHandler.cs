using Dxura.RP.Shared;
namespace Dxura.RP.Game;

public class RankAssignmentActionHandler : ActionHandler<RankAssignmentActionDto>
{
	protected override void Execute( RankAssignmentActionDto action )
	{
		RankSystem.Instance.SetPlayerRanks( action.PlayerId, action.RankIds );
	}
}
