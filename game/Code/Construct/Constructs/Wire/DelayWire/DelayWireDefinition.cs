namespace Dxura.RP.Game.Wire;

public class DelayWireDefinition : ConstructDefinition<DelayWire, DelayWireData>
{
	public override ConstructType Type => ConstructType.DelayWire;
	public override uint Limit => Config.Current.Game.DelayWireLimit;

	public const int MinDelayWireDelay = 1; // 5 second minimum
	public const int MaxDelayWireDelay = 60; // 60 second maximum

	protected override ConstructDataValidationResult ValidateTyped( DelayWireData data )
	{
		if ( data.Delay is < MinDelayWireDelay or > MaxDelayWireDelay )
		{
			return ConstructDataValidationResult.Failure( $"Delay must be between {MinDelayWireDelay} and {MaxDelayWireDelay} seconds" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( DelayWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Delay" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		gameObject.Components.Create<DelayWire>();

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
