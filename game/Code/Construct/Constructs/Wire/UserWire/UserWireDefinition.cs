using Dxura.RP.Game.Tools;
using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Wire;

public class UserWireDefinition : ConstructDefinition<UserWire, UserWireData>
{
	public override ConstructType Type => ConstructType.UserWire;
	public override uint Limit => Config.Current.Game.UserWireLimit;

	public const float MinUserLaserWireRange = 10f;
	public const float MaxUserLaserWireRange = 500f;
	public const float UserWireLineWidth = 0.1f;
	public const float DefaultUserLaserWireRange = 25f;

	protected override ConstructDataValidationResult ValidateTyped( UserWireData data )
	{
		if ( data.Range is < MinUserLaserWireRange or > MaxUserLaserWireRange )
		{
			return ConstructDataValidationResult.Failure( $"Range must be between {MinUserLaserWireRange} and {MaxUserLaserWireRange} units" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( UserWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "User Wire" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		var endLaserTarget = new GameObject( gameObject, true, "End Laser Target" )
		{
			WorldPosition = position + Vector3.Up * data.Range
		};

		var userWire = gameObject.Components.Create<UserWire>();
		userWire.EndLaserTarget = endLaserTarget;

		// Create a simple box model for the laser device
		var model = Model.Load( "models/sbox_props/lit_bollard/lit_bollard_base.vmdl" );

		var modelRenderer = gameObject.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		var lineRenderer = gameObject.Components.Create<LineRenderer>();
		lineRenderer.Points = [gameObject, endLaserTarget];
		lineRenderer.Color = Color.White;
		lineRenderer.Width = UserWireLineWidth;
		lineRenderer.Face = SceneLineObject.FaceMode.Cylinder;
		lineRenderer.EndCap = SceneLineObject.CapStyle.Rounded;

		userWire.LineRenderer = lineRenderer;

		gameObject.Components.Create<ModelCollider>();

		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
