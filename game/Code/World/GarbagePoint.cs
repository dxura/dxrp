namespace Dxura.RP.Game;

/// <summary>
///     A spawn point for garbage
/// </summary>
public sealed class GarbagePoint : Component
{
	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		var spawnpointModel = Model.Load( "models/editor/cordon_helper.vmdl_c" );

		Gizmo.Hitbox.Model( spawnpointModel );
		var so = Gizmo.Draw.Model( spawnpointModel );
		if ( so is not null )
		{
			so.Flags.CastShadows = true;
		}
	}
}
