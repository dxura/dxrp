namespace Dxura.RP.Game.Wire;

public class MetaWireDefinition : WireConstructDefinition<MetaWire, MetaWireData>
{
	public override ConstructType Type => ConstructType.MetaWire;
	public override uint Limit => Config.Current.Game.MetaWireLimit;

	protected override ConstructDataValidationResult ValidateWireTyped( MetaWireData data )
	{
		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( MetaWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Meta" )
		{
			WorldPosition = position,
			WorldRotation = rotation
		};

		gameObject.Components.Create<MetaWire>();

		var model = Model.Load( "models/sbox_props/intruder_alarm_3/intruder_alarm_3.vmdl" );

		var modelRenderer = gameObject.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		var collider = gameObject.Components.Create<ModelCollider>();
		collider.Model = model;

		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
