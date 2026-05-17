using Dxura.RP.Game.Tools;

namespace Dxura.RP.Game.Wire;

public class LedWireDefinition : ConstructDefinition<LedWire, LedWireData>
{
	public override ConstructType Type => ConstructType.LedWire;
	public override uint Limit => Config.Current.Game.LedWireLimit;

	protected override ConstructDataValidationResult ValidateTyped( LedWireData data )
	{
		// LED data validation is minimal - just check colors are valid
		if ( data.OffColor.a <= 0 )
		{
			return ConstructDataValidationResult.Failure( "Off color must have alpha > 0" );
		}

		if ( data.OnColor.a <= 0 )
		{
			return ConstructDataValidationResult.Failure( "On color must have alpha > 0" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( LedWireData data, Vector3 position, Rotation rotation )
	{
		// Create the LED wire construct
		var gameObject = new GameObject( true, "LED" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		// Add the main LedWire component
		var ledWire = gameObject.Components.Create<LedWire>();

		var model = Model.Load( "models/sbox_props/ceiling_halogen/ceiling_halogen.vmdl" );

		// Add a model renderer
		var modelRenderer = gameObject.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.Tint = data.OffColor; // Start with off color
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		ledWire.ModelRenderer = modelRenderer;

		// Add a collider
		var collider = gameObject.Components.Create<ModelCollider>();
		collider.Model = model;

		// Add construct tags
		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
