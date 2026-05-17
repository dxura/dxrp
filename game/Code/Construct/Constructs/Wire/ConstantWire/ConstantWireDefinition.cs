using Dxura.RP.Game.Tools;
using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Wire;

public class ConstantWireDefinition : ConstructDefinition<ConstantWire, ConstantWireData>
{
	public override ConstructType Type => ConstructType.ConstantWire;
	public override uint Limit => Config.Current.Game.ConstantWireLimit;

	public const float DefaultConstantFloat = 0f;
	public const int DefaultConstantInt = 0;

	protected override ConstructDataValidationResult ValidateTyped( ConstantWireData data )
	{
		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( ConstantWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Constant" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		gameObject.Components.Create<ConstantWire>();

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
