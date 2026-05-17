namespace Dxura.RP.Game.Wire;

public class GateWireDefinition : ConstructDefinition<GateWire, GateWireData>
{
	public override ConstructType Type => ConstructType.GateWire;
	public override uint Limit => Config.Current.Game.GateWireLimit;

	public const float GateToleranceThreshold = 0.05f;
	public const float GateEqualityTolerance = 0.001f;
	public const float GateBooleanThreshold = 0.5f;
	public const float GateLogicHigh = 1f;
	public const float GateLogicLow = 0f;

	protected override ConstructDataValidationResult ValidateTyped( GateWireData data )
	{
		if ( !Enum.IsDefined( typeof( GateType ), data.Type ) )
		{
			return ConstructDataValidationResult.Failure( "Invalid gate type" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( GateWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Gate" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		gameObject.Components.Create<GateWire>();

		var model = Model.Load( "models/sbox_props/intruder_alarm_2/intruder_alarm_2.vmdl" );

		var modelRenderer = gameObject.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		var collider = gameObject.Components.Create<ModelCollider>();
		collider.Model = model;

		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
