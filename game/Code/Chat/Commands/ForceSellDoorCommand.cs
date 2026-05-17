using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class ForceSellDoorCommand : ICommand
{
	public const string Name = "forceselldoor";

	public string Command => Name;
	public string Help => Language.GetPhrase( "command.forceselldoor.help" );
	public bool IsUsableWhileDead => false;
	public Permission[] RequiredPermissions => [Permission.CommandForceSellDoor];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		var tr = Sandbox.Game.ActiveScene.Trace.Ray( caller.AimRay, Config.Current.Game.ReachDistance )
			.IgnoreGameObjectHierarchy( caller.GameObject )
			.UseHitboxes()
			.Run();

		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			caller.Error( Language.GetPhrase( "command.forceselldoor.not_door" ) );
			return true;
		}

		var door = tr.GameObject.Root.GetComponentInChildren<Door>();
		if ( !door.IsValid() )
		{
			caller.Error( Language.GetPhrase( "command.forceselldoor.not_door" ) );
			return true;
		}

		if ( door.Owner == 0 )
		{
			caller.Error( Language.GetPhrase( "command.forceselldoor.not_owned" ) );
			return true;
		}

		var previousOwner = door.Owner;
		door.ForceSell();

		caller.Success( Language.GetPhrase( "command.forceselldoor.success" ) );
		Log.Info( $"[COMMAND] {caller.SteamName} ({caller.SteamId}) force sold door owned by {previousOwner}" );
		_ = ServerApiClient.Audit( "ForceSellDoor", $"{caller.SteamName} ({caller.SteamId}) force sold door (previous owner steamId: {previousOwner})", caller.SteamId );

		return true;
	}
}
