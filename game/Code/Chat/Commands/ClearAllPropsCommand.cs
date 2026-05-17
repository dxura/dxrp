using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class ClearAllPropsCommand : ICommand
{
	public string Command => "clearallprops";
	public string Help => Language.GetPhrase( "command.clearallprops.help" );
	public bool IsUsableWhileDead => true;
	public float? CooldownOverride => 0f;
	public Permission[] RequiredPermissions => [Permission.CommandClearAllProps];

	private const string ConfirmKey = "clearallprops:confirm";
	private const float ConfirmWindow = 10f;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !Cooldown.Current.CheckAndStartCooldown( ConfirmKey, ConfirmWindow ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.clearallprops.confirm" ) );
			return true;
		}

		Cooldown.Current.CancelCooldown( ConfirmKey );
		CleanupSystem.Current.CleanupAllConstructs( ConstructType.Prop );
		caller.Success( Language.GetPhrase( "command.clearallprops.success" ) );
		_ = ServerApiClient.Audit( "ClearAllProps", $"{caller.SteamName} ({caller.SteamId}) cleared all props", caller.SteamId );
		return true;
	}
}
