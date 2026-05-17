namespace Dxura.RP.Game;

/// <summary>
/// Dictates where players will spawn when they join DXRP
/// </summary>
[Title( "DXRP Spawn Point" )]
[Category( "Game" )]
[Icon( "accessibility_new" )]
[EditorHandle( "materials/gizmo/spawnpoint.png" )]
public sealed class WorldSpawnPoint : Component
{
	[Property] public string? JobIdentifier { get; set; }

	public GameModeJobDto? Job => string.IsNullOrWhiteSpace( JobIdentifier )
		? null
		: GameModeJobs.FindByReference( JobIdentifier );

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		var color = Job != null ? Job.ColorValue() : (Color)"#E3510D";
		var spawnpointModel = Model.Load( "models/editor/spawnpoint.vmdl" );

		Gizmo.Hitbox.Model( spawnpointModel );
		Gizmo.Draw.Color = color.WithAlpha( Gizmo.IsHovered || Gizmo.IsSelected ? 0.7f : 0.5f );
		var so = Gizmo.Draw.Model( spawnpointModel );
		if ( so is not null )
		{
			so.Flags.CastShadows = true;
		}
	}
}
