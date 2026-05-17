namespace Dxura.RP.Game.Commands;

public class OpenMicCommand : ICommand
{
	public string Command => "openmic";
	public string Help => Language.GetPhrase( "command.openmic.help" );
	public bool IsUsableWhileDead => true;
	public bool IsUsableWhileRestricted => true;	

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		caller.ToggleOpenMic();

		return true;
	}
}
