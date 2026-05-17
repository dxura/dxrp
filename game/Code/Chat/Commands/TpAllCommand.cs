using Dxura.RP.Shared;
namespace Dxura.RP.Game.Commands;

public class TpAllCommand : ICommand
{
	public string Command => "tpall";
	public string Help => "/tpall - Teleport all players to you";
	public Permission[] RequiredPermissions => [Permission.PlayerTeleportAll];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		var callerPosition = caller.WorldPosition + caller.BodyRoot.WorldRotation.Forward * 80f;
		var callerRotation = caller.GameObject.WorldRotation;
		var count = 0;

		foreach ( var player in GameUtils.Players )
		{
			if ( !player.IsValid() || player == caller )
			{
				continue;
			}

			player.TeleportHost( new Transform( callerPosition, callerRotation ) );
			count++;
		}

		caller.Success( string.Format( Language.GetPhrase( "command.tpall.success" ), count ) );
		Log.Info( $"Staff {caller.DisplayName} teleported all players ({count}) to their location" );
		_ = ServerApiClient.Audit( "Teleport", $"{caller.SteamName} ({caller.SteamId}) teleported all players ({count}) to their location", caller.SteamId );

		return true;
	}
}
