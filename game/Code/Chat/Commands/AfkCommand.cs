namespace Dxura.RP.Game.Commands;

public class AfkCommand : ICommand
{
	public string Command => "afk";
	public string Help => "Marks yourself as AFK (away from keyboard) till you move again";
	public bool IsUsableWhileDead => false;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( caller.HasStatus( Constants.AfkStatus ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.afk.already" ) );
			return true;
		}

		if ( AfkSystem.Instance.IsValid() )
		{
			AfkSystem.Instance.ForceAfk( caller );
		}

		caller.SendMessage( Language.GetPhrase( "command.afk.enabled" ) );

		return true;
	}
}
