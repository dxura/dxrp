using Dxura.RP.Shared;
namespace Dxura.RP.Game.Commands;

public class GodCommand : ICommand
{
	public string Command => "god";
	public string Help => "Toggles god mode, making you invincible.";
	public bool IsUsableWhileDead => false;
	public Permission[] RequiredPermissions => [Permission.CommandGodMode];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( caller.HasStatus( Constants.GodStatus ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.god.disabled" ) );
			caller.RemoveStatus( Constants.GodStatus );
		}
		else
		{
			caller.SendMessage( Language.GetPhrase( "command.god.enabled" ) );
			caller.AddStatus( Constants.GodStatus );
		}

		return true;
	}
}
