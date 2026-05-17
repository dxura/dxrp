namespace Dxura.RP.Game.Commands;

public class SellAllDoorsCommand : ICommand
{
	public const string Name = "sellalldoors";
	public string Command => Name;
	public string Help => "/sellalldoors - Sell all doors you own";

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:sellalldoors", Config.Current.Game.SellAllDoorsCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		// Find all doors owned by the player
		var ownedDoors = Sandbox.Game.ActiveScene.GetAllComponents<Door>()
			.Where( d => d.Owner == caller.SteamId )
			.ToList();

		if ( !ownedDoors.Any() )
		{
			caller.Error( Language.GetPhrase( "command.selldoors.none" ) );
			return true;
		}

		// Sell all owned doors
		foreach ( var door in ownedDoors )
		{
			door.ForceSell();
		}

		caller.Success( string.Format( Language.GetPhrase( "command.selldoors.success" ), ownedDoors.Count ) );
		Log.Info( $"Player {caller.SteamId} sold all doors ({ownedDoors.Count} doors)" );

		return true;
	}
}
