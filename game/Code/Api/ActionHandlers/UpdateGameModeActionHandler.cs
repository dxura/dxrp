using Dxura.RP.Shared;
namespace Dxura.RP.Game;

public class UpdateGameModeActionHandler : ActionHandler<UpdateGameModeActionDto>
{
	protected override void Execute( UpdateGameModeActionDto action )
	{
		Config.Current.SetGameMode(  action.GameMode );
		Log.Info( $"Game mode updated: {action.GameMode.Name} ({action.GameMode.Jobs.Count} jobs, {action.GameMode.JobGroups.Count} groups)" );
	}
}
