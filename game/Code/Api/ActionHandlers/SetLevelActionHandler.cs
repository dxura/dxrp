using Dxura.RP.Shared;
namespace Dxura.RP.Game;

public class SetLevelActionHandler : ActionHandler<SetLevelActionDto>
{
	protected override void Execute( SetLevelActionDto action )
	{
		var player = GameUtils.GetPlayerById( action.PlayerId );
		if ( !player.IsValid() )
		{
			return;
		}

		player.Level = action.Level;
		Log.Info( $"Set level for {player.DisplayName} ({player.SteamId}) to {action.Level}" );
	}
}
