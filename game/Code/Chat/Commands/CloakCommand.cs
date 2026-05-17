using Dxura.RP.Shared;
namespace Dxura.RP.Game.Commands;

public class CloakCommand : ICommand
{
	public string Command => "cloak";
	public string Help => "Cloaks your character, making you invisible to others.";
	public bool IsUsableWhileDead => false;
	public Permission[] RequiredPermissions => [Permission.CommandCloak];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( caller.HasStatus( Constants.CloakStatus ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.cloak.visible" ) );
			caller.RemoveStatus( Constants.CloakStatus );
		}
		else
		{
			caller.SendMessage( Language.GetPhrase( "command.cloak.cloaked" ) );
			caller.AddStatus( Constants.CloakStatus );
		}

		return true;
	}
}
