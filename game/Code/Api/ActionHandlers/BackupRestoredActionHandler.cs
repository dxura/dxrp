using Dxura.RP.Game.UI;
using Dxura.RP.Shared;
namespace Dxura.RP.Game;

public class BackupRestoredActionHandler : ActionHandler<BackupRestoredActionDto>
{
	protected override void Execute( BackupRestoredActionDto action )
	{
		GameManager.Instance.BroadcastAnnouncementHost(
			Language.GetPhrase( "backup.restored" ),
			Announcement.AnnouncementType.System,
			15
		);

		foreach ( var data in action.Players )
		{
			var player = GameUtils.GetPlayerById( data.PlayerId );
			if ( !player.IsValid() ) continue;

			player.SetBankBalanceHost( data.Balance );

			if ( data.Level.HasValue )
				player.Level = data.Level.Value;
		}

		Log.Info( $"[Backup] Restored balances and levels for {action.Players.Count} players" );
	}
}
