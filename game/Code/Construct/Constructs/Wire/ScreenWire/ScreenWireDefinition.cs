namespace Dxura.RP.Game.Wire;

public class ScreenWireDefinition : ConstructDefinition<ScreenWire, ScreenWireData>
{
	public override ConstructType Type => ConstructType.ScreenWire;
	public override uint Limit => Config.Current.Game.ScreenWireLimit;

	public const int MinScreenSize = 5;
	public const int MaxScreenSize = 200;
	public const int MaxScreenLabelLength = 50;
	public const int DefaultScreenWidth = 30;
	public const int DefaultScreenHeight = 30;
	public const float ScreenBackingThickness = 0.5f;
	public const float ScreenDisplayOffset = 0.26f;
	public const int ScreenCameraTextureSize = 512;
	public const float ScreenDefaultFontSize = 38f;

	protected override ConstructDataValidationResult ValidateTyped( ScreenWireData data )
	{
		if ( data.Width is < MinScreenSize or > MaxScreenSize )
		{
			return ConstructDataValidationResult.Failure( $"Width must be between {MinScreenSize} and {MaxScreenSize}" );
		}

		if ( data.Height is < MinScreenSize or > MaxScreenSize )
		{
			return ConstructDataValidationResult.Failure( $"Height must be between {MinScreenSize} and {MaxScreenSize}" );
		}

		if ( string.IsNullOrWhiteSpace( data.Label ) )
		{
			return ConstructDataValidationResult.Failure( "Label cannot be empty" );
		}

		if ( data.Label.Length > MaxScreenLabelLength )
		{
			return ConstructDataValidationResult.Failure( $"Label cannot exceed {MaxScreenLabelLength} characters" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( ScreenWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Screen" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		var screenWire = gameObject.Components.Create<ScreenWire>();

		var backingRenderer = screenWire.BackingRenderer = gameObject.Components.Create<ModelRenderer>();
		backingRenderer.Tint = Color.Green;
		backingRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		var displayRenderer = screenWire.DisplayRenderer = gameObject.Components.Create<ModelRenderer>();
		displayRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		var collider = gameObject.Components.Create<BoxCollider>();
		collider.Scale = new Vector3( data.Width, data.Height, ScreenBackingThickness );

		var valueGameObject = new GameObject( gameObject )
		{
			Name = "Value", LocalPosition = new Vector3( 0, 0, ScreenBackingThickness * 1.05f ), LocalRotation = Rotation.FromPitch( 90 )
		};

		var textRenderer = valueGameObject.Components.Create<TextRenderer>( false );
		screenWire.ValueTextRenderer = textRenderer;

		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag, Constants.CostlyTag );

		return gameObject;
	}
}
