using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class ClearAllEntitiesCommand : ICommand
{
	public string Command => "clearallentities";
	public string Help => Language.GetPhrase( "command.clearallentities.help" );
	public bool IsUsableWhileDead => true;
	public float? CooldownOverride => 0f;
	public Permission[] RequiredPermissions => [Permission.CommandClearAllEntities];

	private const string ConfirmKey = "clearallentities:confirm";
	private const float ConfirmWindow = 10f;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !Cooldown.Current.CheckAndStartCooldown( ConfirmKey, ConfirmWindow ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.clearallentities.confirm" ) );
			return true;
		}

		Cooldown.Current.CancelCooldown( ConfirmKey );
		CleanupSystem.Current.CleanupAllEntities();
		caller.Success( Language.GetPhrase( "command.clearallentities.success" ) );
		_ = ServerApiClient.Audit( "ClearAllEntities", $"{caller.SteamName} ({caller.SteamId}) cleared all entities", caller.SteamId );
		return true;
	}
}
