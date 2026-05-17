using Dxura.RP.Shared;
namespace Dxura.RP.Game;

public class UpdateServerInfoActionHandler : ActionHandler<UpdateServerInfoActionDto>
{
	protected override void Execute( UpdateServerInfoActionDto action )
	{
		Networking.ServerName = action.Name;
		GameNetworkManager.ServerName = action.Name;
		GameNetworkManager.MaxPlayers = action.MaxPlayers;
		GameNetworkManager.WhitelistRankIds = action.WhitelistRankIds.ToArray();
		ServerApiLink.Current.RulesetId = action.RulesetId;

		// Description is currently unused

		if ( !string.IsNullOrWhiteSpace( action.OverrideConfig ) )
		{
			Config.ApplyOverride( action.OverrideConfig );
		}

		Log.Info( $"Updated server info: Name={action.Name}, MaxPlayers={action.MaxPlayers}, RulesetId={action.RulesetId}, GameModeId={action.GameModeId}" );
	}
}
