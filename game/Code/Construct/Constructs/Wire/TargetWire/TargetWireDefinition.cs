using Dxura.RP.Game.Tools;
using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Wire;

public class TargetWireDefinition : ConstructDefinition<TargetWire, TargetWireData>
{
	public override ConstructType Type => ConstructType.TargetWire;
	public override uint Limit => Config.Current.Game.TargetWireLimit;

	protected override ConstructDataValidationResult ValidateTyped( TargetWireData data )
	{

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( TargetWireData data, Vector3 position, Rotation rotation )
	{
		// Create the speaker wire construct via code, not prefab
		var gameObject = new GameObject( true, "Target" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		// Add the main TargetWire component
		gameObject.Components.Create<TargetWire>();

		var model = Model.Load( "models/wire/target/target.vmdl" );

		// Add a basic model for visualization
		var modelRenderer = gameObject.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		// Add a model collider
		var collider = gameObject.Components.Create<ModelCollider>();
		collider.Model = model;

		// Add a health component
		var health = gameObject.Components.Create<HealthComponent>();
		health.Health = float.MaxValue;
		health.MaxHealth = float.MaxValue;

		// Add construct tags
		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
