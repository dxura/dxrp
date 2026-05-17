using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class GotoCommand : ICommand
{
	public const string Name = "goto";

	public string Command => Name;
	public string Help => Language.GetPhrase( "command.goto.help" );
	public Permission[] RequiredPermissions => [Permission.PlayerTeleport];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( args.Length == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.goto.usage" ) );
			return true;
		}

		var targetPlayer = CommandHelper.ResolvePlayer( caller, string.Join( " ", args ) );
		if ( !targetPlayer.IsValid() )
		{
			return true;
		}

		var oldPosition = caller.GameObject.WorldPosition;
		AdminSystem.Instance.PlayerReturnPositions[caller.SteamId] = (oldPosition, caller.GameObject.WorldRotation);

		var newPosition = targetPlayer.GameObject.WorldPosition + targetPlayer.BodyRoot.WorldRotation.Forward * -50f;
		caller.TeleportHost( new Transform( newPosition, targetPlayer.GameObject.WorldRotation ) );
		OcclusionSystem.Current?.BroadcastForceCheckHost( caller.Connection, targetPlayer.Connection );

		AdminSystem.Instance?.BroadcastTeleportEffect( caller, oldPosition, newPosition );

		caller.Success( string.Format( Language.GetPhrase( "command.goto.success" ), targetPlayer.DisplayName ) );
		_ = ServerApiClient.Audit( "Teleport", $"{caller.SteamName} ({caller.SteamId}) teleported to {targetPlayer.SteamName} ({targetPlayer.SteamId})", caller.SteamId );
		return true;
	}
}
