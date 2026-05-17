using Dxura.RP.Game.Tools;
namespace Dxura.RP.Game.Wire;

public class SynthesizerWireDefinition : ConstructDefinition<SynthesizerWire, SynthesizerWireData>
{
	public override ConstructType Type => ConstructType.SynthesizerWire;
	public override uint Limit => Config.Current.Game.SynthesizerWireLimit;

	protected override ConstructDataValidationResult ValidateTyped( SynthesizerWireData data )
	{
		if ( data.Volume is < 0f or > 1f )
		{
			return ConstructDataValidationResult.Failure( "Volume must be between 0 and 1" );
		}

		if ( data.Pitch is < SpeakerWireTool.MinSpeakerPitch or > SpeakerWireTool.MaxSpeakerPitch )
		{
			return ConstructDataValidationResult.Failure( $"Pitch must be between {SpeakerWireTool.MinSpeakerPitch} and {SpeakerWireTool.MaxSpeakerPitch}" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( SynthesizerWireData data, Vector3 position, Rotation rotation )
	{
		// Create the synthesizer wire construct using the speaker model
		var gameObject = new GameObject( true, "Synthesizer" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		// Add the main SynthesizerWire component
		var synthesizerWire = gameObject.Components.Create<SynthesizerWire>();

		var model = Model.Load( "models/wire/speaker/speaker.vmdl" );

		// Add a basic model for visualization (using speaker model)
		var modelRenderer = gameObject.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		// Add a model collider
		var collider = gameObject.Components.Create<ModelCollider>();
		collider.Model = model;

		// Add construct tags
		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
