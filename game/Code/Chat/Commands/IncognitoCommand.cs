using Dxura.RP.Shared;
namespace Dxura.RP.Game.Commands;

public class IncognitoCommand : ICommand
{
	public string Command => "incognito";
	public string[] Aliases => ["private", "incog"];
	public string Help => "Hides you from the player list.";
	public bool IsUsableWhileDead => true;
	public Permission[] RequiredPermissions => [Permission.CommandIncognito];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( caller.HasStatus( Constants.IncognitoStatus ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.incognito.visible" ) );
			caller.RemoveStatus( Constants.IncognitoStatus );
		}
		else
		{
			caller.SendMessage( Language.GetPhrase( "command.incognito.hidden" ) );
			caller.AddStatus( Constants.IncognitoStatus );
		}

		return true;
	}
}
