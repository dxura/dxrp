using Dxura.RP.Game.UI;
using Dxura.RP.Shared;
namespace Dxura.RP.Game;

public class BroadcastMessageActionHandler : ActionHandler<BroadcastMessageActionDto>
{
	protected override void Execute( BroadcastMessageActionDto action )
	{
		GameManager.Instance.BroadcastAnnouncementHost( action.Message, Announcement.AnnouncementType.Staff, action.DurationSeconds );
	}
}
