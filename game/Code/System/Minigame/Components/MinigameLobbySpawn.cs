namespace Dxura.RP.Game;

/// <summary>
/// Dictates where players will spawn when the minigame lobby starts
/// </summary>
[Title( "Minigame Lobby Spawn Point" )]
[Category( "Game" )]
[Icon( "accessibility_new" )]
[EditorHandle( "materials/gizmo/spawnpoint.png" )]
public sealed class MinigameLobbySpawnPoint : Component
{
	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		var spawnpointModel = Model.Load( "models/editor/spawnpoint.vmdl" );

		Gizmo.Hitbox.Model( spawnpointModel );
		var so = Gizmo.Draw.Model( spawnpointModel );
		if ( so is not null )
		{
			so.Flags.CastShadows = true;
			so.ColorTint = Color.Green;
		}
	}

}
