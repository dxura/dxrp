using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class ReturnCommand : ICommand
{
	public const string Name = "return";

	public string Command => Name;
	public string Help => Language.GetPhrase( "command.return.help" );
	public Permission[] RequiredPermissions => [Permission.PlayerTeleport];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( args.Length == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.return.usage" ) );
			return true;
		}

		var targetPlayer = CommandHelper.ResolvePlayer( caller, string.Join( " ", args ) );
		if ( !targetPlayer.IsValid() )
		{
			return true;
		}

		var returnPositions = AdminSystem.Instance.PlayerReturnPositions;
		if ( !returnPositions.TryGetValue( targetPlayer.SteamId, out var savedTransform ) )
		{
			caller.Error( "#generic.return.position.not.found" );
			return true;
		}

		var oldPosition = targetPlayer.GameObject.WorldPosition;
		targetPlayer.TeleportHost( new Transform( savedTransform.Position, savedTransform.Rotation ) );
		OcclusionSystem.Current?.BroadcastForceCheckHost( caller.Connection, targetPlayer.Connection );
		returnPositions.Remove( targetPlayer.SteamId );

		AdminSystem.Instance?.BroadcastTeleportEffect( targetPlayer, oldPosition, savedTransform.Position );

		caller.Success( string.Format( Language.GetPhrase( "command.return.success" ), targetPlayer.DisplayName ) );
		_ = ServerApiClient.Audit( "Teleport", $"{caller.SteamName} ({caller.SteamId}) returned {targetPlayer.SteamName} ({targetPlayer.SteamId}) to their previous position", caller.SteamId );
		return true;
	}
}
