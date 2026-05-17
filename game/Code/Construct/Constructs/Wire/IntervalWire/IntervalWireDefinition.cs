namespace Dxura.RP.Game.Wire;

public class IntervalWireDefinition : ConstructDefinition<IntervalWire, IntervalWireData>
{
	public override ConstructType Type => ConstructType.IntervalWire;
	public override uint Limit => Config.Current.Game.IntervalWireLimit;

	public const float MinIntervalWireInterval = 0.05f; // permissive UI floor; runtime min is the wire tick rate
	public const float MaxIntervalWireInterval = 3600f; // 1 hour maximum
	public const float MinIntervalWireHold = 0f; // 0 seconds minimum
	public const float MaxIntervalWireHold = 600f; // 10 minutes maximum

	protected override ConstructDataValidationResult ValidateTyped( IntervalWireData data )
	{
		var minInterval = Config.Current.Game.WireTick;

		if ( data.Interval < minInterval || data.Interval > MaxIntervalWireInterval )
		{
			return ConstructDataValidationResult.Failure( $"Interval must be between {minInterval} and {MaxIntervalWireInterval} seconds" );
		}

		if ( data.Hold is < MinIntervalWireHold or > MaxIntervalWireHold )
		{
			return ConstructDataValidationResult.Failure( $"Hold duration must be between {MinIntervalWireHold} and {MaxIntervalWireHold} seconds" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( IntervalWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Interval" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		gameObject.Components.Create<IntervalWire>();

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
