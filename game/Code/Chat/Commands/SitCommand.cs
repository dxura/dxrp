namespace Dxura.RP.Game.Commands;

public class SitCommand : ICommand
{
	public string Command => "sit";
	public string Help => "Sit down or stand up";
	public bool IsUsableWhileDead => false;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( caller.Sitting )
		{
			// No cooldown needed when standing up
			caller.SetSit( null );
		}
		else
		{
			// Check cooldown before attempting to sit
			if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:sit", Config.Current.Game.PlayerSitCooldown ) )
			{
				caller.Error( "#generic.wait" );
				return true;
			}

			caller.SetSit( SitType.Sit );
		}

		return true;
	}
}
