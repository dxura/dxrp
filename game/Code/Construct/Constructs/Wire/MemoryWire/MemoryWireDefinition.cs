namespace Dxura.RP.Game.Wire;

public class MemoryWireDefinition : ConstructDefinition<MemoryWire, MemoryWireData>
{
	public override ConstructType Type => ConstructType.MemoryWire;
	public override uint Limit => Config.Current.Game.MemoryWireLimit;

	protected override ConstructDataValidationResult ValidateTyped( MemoryWireData data )
	{
		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( MemoryWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Memory" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		gameObject.Components.Create<MemoryWire>();

		var model = Model.Load( "models/sbox_props/intruder_alarm_1/intruder_alarm_1.vmdl" );

		var modelRenderer = gameObject.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		var collider = gameObject.Components.Create<ModelCollider>();
		collider.Model = model;

		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
