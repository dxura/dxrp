namespace Dxura.RP.Game.Commands;

public class DropCommand : ICommand
{
	public string Command => "drop";
	public string Help => "/drop - Drop your current weapon";
	public bool IsUsableWhileDead => false;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( !caller.CurrentEquipment.IsValid() )
		{
			caller.SendMessage( Language.GetPhrase( "command.drop.no_weapon" ) );
			return true;
		}

		if ( !caller.CurrentEquipment.CanDrop )
		{
			caller.SendMessage( Language.GetPhrase( "command.drop.cannot_drop" ) );
			return true;
		}

		// Call the private Drop method via reflection or use DropHost directly
		caller.DropHost( caller.CurrentEquipment, false );

		return true;
	}
}
