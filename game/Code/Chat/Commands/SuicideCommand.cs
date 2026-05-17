namespace Dxura.RP.Game.Commands;

public class SuicideCommand : ICommand
{
	public const string CommandConstant = "suicide";
	public string Command => CommandConstant;
	public string Help => "Commits Suicide";
	public bool IsUsableWhileDead => false;
	public string[] Aliases => ["kill"];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:suicide", Config.Current.Game.SuicideCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		if ( !caller.IsValid() || caller.RespawnState != RespawnState.Not || caller.HealthComponent.IsGodMode )
		{
			return false;
		}

		Log.Info( $"Player {caller.SteamId} committed suicide" );

		caller.KillHost();

		return true;
	}
}
