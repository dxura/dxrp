namespace Dxura.RP.Game.Wire;

public class ButtonWireDefinition : ConstructDefinition<ButtonWire, ButtonWireData>
{
	public override ConstructType Type => ConstructType.ButtonWire;
	public override uint Limit => Config.Current.Game.ButtonWireLimit;

	public const float MinButtonValue = -100000f;
	public const float MaxButtonValue = 100000f;
	public const float DefaultButtonOffValue = 0f;
	public const float DefaultButtonOnValue = 1f;

	protected override ConstructDataValidationResult ValidateTyped( ButtonWireData data )
	{
		if ( data.OffValue is < MinButtonValue or > MaxButtonValue )
		{
			return ConstructDataValidationResult.Failure( $"Off Value must be between {MinButtonValue} and {MaxButtonValue}" );
		}

		if ( data.OnValue is < MinButtonValue or > MaxButtonValue )
		{
			return ConstructDataValidationResult.Failure( $"On Value must be between {MinButtonValue} and {MaxButtonValue}" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( ButtonWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Button" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		var buttonWire = gameObject.Components.Create<ButtonWire>();

		var modelGameObject = new GameObject( gameObject )
		{
			Name = "Model", LocalPosition = Vector3.Zero, LocalRotation = Rotation.Identity, NetworkMode = NetworkMode.Snapshot
		};

		var model = Model.Load( "models/props/big_button/big_button.vmdl" );

		var modelRenderer = modelGameObject.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		var collider = gameObject.Components.Create<ModelCollider>();
		collider.Model = model;

		buttonWire.ModelGameObject = modelGameObject;

		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
