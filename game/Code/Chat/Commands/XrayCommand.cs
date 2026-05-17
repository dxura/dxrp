using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class XrayCommand : ICommand
{
	public string Command => "xray";
	public string Help => Language.GetPhrase( "command.xray.help" );
	public Permission[] RequiredPermissions => [Permission.CommandXray];

	private long _targetSteamId;
	private bool _active;

	public void OnFrame()
	{
		if ( !_active ) return;

		var cam = Sandbox.Game.ActiveScene?.Camera;
		if ( !cam.IsValid() ) return;

		var camPos = cam.WorldPosition;

		Gizmo.Draw.IgnoreDepth = true;

		foreach ( var entity in Sandbox.Game.ActiveScene?.GetAll<BaseEntity>() ?? [] )
		{
			if ( _targetSteamId != 0 && entity.Owner != _targetSteamId ) continue;

			var worldPos = entity.WorldPosition;
			var dist = Vector3.DistanceBetween( camPos, worldPos );
			var owner = _targetSteamId == 0 ? GameUtils.GetPlayerById( entity.Owner )?.DisplayName ?? entity.Owner.ToString() : null;
			var label = owner != null ? $"{entity.DisplayName}\n{owner}\n{dist / 39.37f:F0}m" : $"{entity.DisplayName}\n{dist / 39.37f:F0}m";

			Gizmo.Draw.Color = entity.Color;
			Gizmo.Draw.Text( label, new Transform( worldPos ) );
		}

		foreach ( var player in GameUtils.Players )
		{
			if ( !player.IsValid() || player == Player.Local ) continue;
			if ( _targetSteamId != 0 && player.SteamId != _targetSteamId ) continue;

			var worldPos = player.WorldPosition;
			var dist = Vector3.DistanceBetween( camPos, worldPos );

			Gizmo.Draw.Color = Color.Cyan;
			Gizmo.Draw.LineBBox( new BBox( worldPos + new Vector3( -16, -16, 0 ), worldPos + new Vector3( 16, 16, 72 ) ) );

			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.Text( $"{player.DisplayName}\n{dist / 39.37f:F0}m", new Transform( worldPos + Vector3.Up * 80 ) );
		}

		Gizmo.Draw.IgnoreDepth = false;
	}

	public bool ExecuteLocal( string[] args, string raw )
	{
		if ( !Player.Local.IsValid() )
			return false;

		if ( !RankSystem.HasPermission( Player.Local.SteamId, Permission.CommandXray ) )
		{
			Player.Local.SendMessage( "#generic.permission" );
			return true;
		}

		if ( _active && args.Length == 0 )
		{
			_active = false;
			_targetSteamId = 0;
			Player.Local.Success( Language.GetPhrase( "command.xray.cleared" ) );
			return true;
		}

		if ( args.Length == 0 )
		{
			_active = true;
			_targetSteamId = 0;
			Player.Local.Success( Language.GetPhrase( "command.xray.all" ) );
			return true;
		}

		var target = CommandHelper.ResolvePlayer( Player.Local, string.Join( " ", args ) );
		if ( target == null ) return true;

		if ( _active && _targetSteamId == target.SteamId )
		{
			_active = false;
			_targetSteamId = 0;
			Player.Local.Success( Language.GetPhrase( "command.xray.cleared" ) );
			return true;
		}

		_active = true;
		_targetSteamId = target.SteamId;
		Player.Local.Success( string.Format( Language.GetPhrase( "command.xray.enabled" ), target.DisplayName ) );
		return true;
	}

	public bool ExecuteHost( Player caller, string[] args, string raw ) => false;
}
