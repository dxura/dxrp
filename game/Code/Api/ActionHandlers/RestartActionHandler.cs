using Dxura.RP.Shared;
namespace Dxura.RP.Game;

public class RestartActionHandler : ActionHandler<RestartServerActionDto>
{
	protected override void Execute( RestartServerActionDto action )
	{
		_ = AdminSystem.Instance.Restart( action.Reason ?? "No Reason Provided", action.RefundEntities, action.DelaySeconds );
	}
}
