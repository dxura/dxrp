namespace Dxura.RP.Game.Wire;

public class PressurePlateWireDefinition : WireConstructDefinition<PressurePlateWire, PressurePlateWireData>
{
	public override ConstructType Type => ConstructType.PressurePlateWire;
	public override uint Limit => Config.Current.Game.PressurePlateWireLimit;

	public const int MinSize = 5;
	public const int MaxSize = 200;
	public const int MinDepth = 1;
	public const int MaxDepth = 30;
	public const int DefaultWidth = 30;
	public const int DefaultLength = 30;
	public const int DefaultDepth = 4;
	public const float DetectionHeight = 8f;
	public const float SurfaceTolerance = 1f;
	public const float StackSearchHeight = 500f;
	public const float StackSupportGap = 3f;
	public const int MaxStackPasses = 64;
	public const float FlatSpawnBuffer = 1f;
	public const float PressDepthRatio = 0.5f;
	public const float MaxPressDepth = 10f;
	public const float ReferenceMass = 500f;
	public const float PlayerBodyVolume = 32f * 32f * 72f;
	public const float MassPerVolumeUnit = ReferenceMass / PlayerBodyVolume;
	public const float MinPressDepth = 0.1f;
	public static readonly Color RestPlateColor = Color.FromRgb( 0x888888 );
	public static readonly Color PressedPlateColor = Color.FromRgb( 0x4CAF50 );

	public static float GetMaxPressDepth( int depth )
	{
		return Math.Min( depth * PressDepthRatio, MaxPressDepth );
	}

	public static float GetPressDepthFromMass( float totalMass, int plateDepth )
	{
		if ( totalMass <= 0f )
		{
			return 0f;
		}

		var maxPress = GetMaxPressDepth( plateDepth );
		var factor = Math.Clamp( totalMass / ReferenceMass, 0f, 1f );
		return Math.Max( MinPressDepth, maxPress * factor );
	}

	protected override ConstructDataValidationResult ValidateWireTyped( PressurePlateWireData data )
	{
		if ( data.Width is < MinSize or > MaxSize )
		{
			return ConstructDataValidationResult.Failure( $"Width must be between {MinSize} and {MaxSize}" );
		}

		if ( data.Length is < MinSize or > MaxSize )
		{
			return ConstructDataValidationResult.Failure( $"Length must be between {MinSize} and {MaxSize}" );
		}

		if ( data.Depth is < MinDepth or > MaxDepth )
		{
			return ConstructDataValidationResult.Failure( $"Depth must be between {MinDepth} and {MaxDepth}" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( PressurePlateWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Pressure Plate" )
		{
			WorldPosition = position,
			WorldRotation = rotation
		};

		var pressurePlate = gameObject.Components.Create<PressurePlateWire>();

		var plateModel = new GameObject( gameObject, true, "Plate" )
		{
			LocalPosition = Vector3.Zero,
			LocalRotation = Rotation.Identity,
			NetworkMode = NetworkMode.Never
		};
		pressurePlate.PlateModel = plateModel;
		pressurePlate.PlateRenderer = plateModel.Components.Create<ModelRenderer>();
		pressurePlate.PlateRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;
		pressurePlate.PlateCollider = plateModel.Components.Create<BoxCollider>();

		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
