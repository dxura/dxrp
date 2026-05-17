using Dxura.RP.Game.Tools;
using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Wire;

public class TriggerWireDefinition : ConstructDefinition<TriggerWire, TriggerWireData>
{
	public override ConstructType Type => ConstructType.TriggerWire;
	public override uint Limit => Config.Current.Game.TriggerWireLimit;

	public const float MinTriggerLaserWireRange = 10f;
	public const float MaxTriggerLaserWireRange = 500f;
	public const float TriggerWireLineWidth = 0.1f;
	public const float DefaultTriggerLaserWireRange = 25f;

	protected override ConstructDataValidationResult ValidateTyped( TriggerWireData data )
	{
		if ( data.Range is < MinTriggerLaserWireRange or > MaxTriggerLaserWireRange )
		{
			return ConstructDataValidationResult.Failure( $"Range must be between {MinTriggerLaserWireRange} and {MaxTriggerLaserWireRange} units" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( TriggerWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Trigger Wire" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		var endLaserTarget = new GameObject( gameObject, true, "End Laser Target" )
		{
			WorldPosition = position + Vector3.Up * data.Range
		};

		var triggerWire = gameObject.Components.Create<TriggerWire>();
		triggerWire.EndLaserTarget = endLaserTarget;

		// Create a simple box model for the laser device
		var model = Model.Load( "models/sbox_props/lit_bollard/lit_bollard_base.vmdl" );

		var modelRenderer = gameObject.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		var lineRenderer = gameObject.Components.Create<LineRenderer>();
		lineRenderer.Points = [gameObject, endLaserTarget];
		lineRenderer.Color = Color.Red; // Red color to indicate it's a trigger
		lineRenderer.Width = TriggerWireLineWidth;
		lineRenderer.Face = SceneLineObject.FaceMode.Cylinder;
		lineRenderer.EndCap = SceneLineObject.CapStyle.Rounded;

		triggerWire.LineRenderer = lineRenderer;

		gameObject.Components.Create<ModelCollider>();

		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
