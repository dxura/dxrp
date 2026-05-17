using Dxura.RP.Game.Tools;
using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Wire;

public class ForcerWireDefinition : ConstructDefinition<ForcerWire, ForcerWireData>
{
	public override ConstructType Type => ConstructType.ForcerWire;
	public override uint Limit => Config.Current.Game.ForcerWireLimit;

	public const float MinForcerWireForce = 1f;
	public const float MaxForcerWireForce = 2500f;
	public const float MinForcerLaserWireRange = 10f;
	public const float MaxForcerLaserWireRange = 500f;
	public const float ForcerWireLineWidth = 0.1f;
	public const float DefaultForcerLaserWireRange = 25f;

	protected override ConstructDataValidationResult ValidateTyped( ForcerWireData data )
	{
		if ( data.ForceAmount is < MinForcerWireForce or > MaxForcerWireForce )
		{
			return ConstructDataValidationResult.Failure( $"Force Amount must be between {MinForcerWireForce} and {MaxForcerWireForce}" );
		}

		if ( data.Range is < MinForcerLaserWireRange or > MaxForcerLaserWireRange )
		{
			return ConstructDataValidationResult.Failure( $"Range must be between {MinForcerLaserWireRange} and {MaxForcerLaserWireRange} units" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( ForcerWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Forcer" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		var endLaserTarget = new GameObject( gameObject, true, "End Laser Target" )
		{
			WorldPosition = position + Vector3.Up * data.Range
		};

		var forcerWire = gameObject.Components.Create<ForcerWire>();
		forcerWire.EndLaserTarget = endLaserTarget;

		// Create a simple box model for the laser device
		var model = Model.Load( "models/sbox_props/lit_bollard/lit_bollard_base.vmdl" );

		var modelRenderer = gameObject.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		var lineRenderer = gameObject.Components.Create<LineRenderer>();
		lineRenderer.Points = [gameObject, endLaserTarget];
		lineRenderer.Color = Color.Orange;
		lineRenderer.Width = ForcerWireLineWidth;
		lineRenderer.Face = SceneLineObject.FaceMode.Cylinder;
		lineRenderer.EndCap = SceneLineObject.CapStyle.Rounded;

		forcerWire.LineRenderer = lineRenderer;

		gameObject.Components.Create<ModelCollider>();

		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
