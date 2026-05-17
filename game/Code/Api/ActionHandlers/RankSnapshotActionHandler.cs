using Dxura.RP.Shared;
namespace Dxura.RP.Game;

public class RankSnapshotActionHandler : ActionHandler<UpdateRanksActionDto>
{
	protected override void Execute( UpdateRanksActionDto action )
	{
		RankSystem.Instance.SetRanks( action.Ranks );
	}
}
