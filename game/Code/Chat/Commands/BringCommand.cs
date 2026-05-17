using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class BringCommand : ICommand
{
	public const string Name = "bring";

	public string Command => Name;
	public string Help => Language.GetPhrase( "command.bring.help" );
	public Permission[] RequiredPermissions => [Permission.PlayerTeleport];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( args.Length == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.bring.usage" ) );
			return true;
		}

		var targetPlayer = CommandHelper.ResolvePlayer( caller, string.Join( " ", args ) );
		if ( !targetPlayer.IsValid() )
		{
			return true;
		}

		var oldPosition = targetPlayer.GameObject.WorldPosition;
		AdminSystem.Instance.PlayerReturnPositions[targetPlayer.SteamId] = (oldPosition, targetPlayer.GameObject.WorldRotation);

		var newPosition = caller.WorldPosition + caller.BodyRoot.WorldRotation.Forward * 80f;
		targetPlayer.TeleportHost( new Transform( newPosition, caller.GameObject.WorldRotation ) );
		OcclusionSystem.Current?.BroadcastForceCheckHost( caller.Connection, targetPlayer.Connection );

		AdminSystem.Instance?.BroadcastTeleportEffect( targetPlayer, oldPosition, newPosition );

		caller.Success( string.Format( Language.GetPhrase( "command.bring.success" ), targetPlayer.DisplayName ) );
		_ = ServerApiClient.Audit( "Teleport", $"{caller.SteamName} ({caller.SteamId}) brought {targetPlayer.SteamName} ({targetPlayer.SteamId}) to them", caller.SteamId );
		return true;
	}
}
