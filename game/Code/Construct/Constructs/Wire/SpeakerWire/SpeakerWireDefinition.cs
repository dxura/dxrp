using Dxura.RP.Game.Tools;
using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Wire;

public class SpeakerWireDefinition : ConstructDefinition<SpeakerWire, SpeakerWireData>
{
	public override ConstructType Type => ConstructType.SpeakerWire;
	public override uint Limit => Config.Current.Game.SpeakerWireLimit;

	public const float DefaultSpeakerVolume = 0.4f;
	public const float DefaultSpeakerPitch = 1.0f;
	public const float DefaultSpeakerDistance = 800f;

	protected override ConstructDataValidationResult ValidateTyped( SpeakerWireData data )
	{
		if ( data.Volume is < SpeakerWireTool.MinSpeakerVolume or > SpeakerWireTool.MaxSpeakerVolume )
		{
			return ConstructDataValidationResult.Failure( $"Volume must be between {SpeakerWireTool.MinSpeakerVolume} and {SpeakerWireTool.MaxSpeakerVolume}" );
		}

		if ( data.Pitch is < SpeakerWireTool.MinSpeakerPitch or > SpeakerWireTool.MaxSpeakerPitch )
		{
			return ConstructDataValidationResult.Failure( $"Pitch must be between {SpeakerWireTool.MinSpeakerPitch} and {SpeakerWireTool.MaxSpeakerPitch}" );
		}

		if ( data.Distance is < SpeakerWireTool.MinSpeakerDistance or > SpeakerWireTool.MaxSpeakerDistance )
		{
			return ConstructDataValidationResult.Failure( $"Distance must be between {SpeakerWireTool.MinSpeakerDistance} and {SpeakerWireTool.MaxSpeakerDistance}" );
		}

		// Verify approved sound using DropdownPropertyAttribute from the tool
		if ( string.IsNullOrEmpty( data.Sound ) )
		{
			return ConstructDataValidationResult.Failure( "Sound cannot be empty" );
		}

		// Get the allowed sounds from the SpeakerWireTool's DropdownPropertyAttribute using TypeLibrary
		var toolTypeDescription = TypeLibrary.GetType<SpeakerWireTool>();
		var soundProperty = toolTypeDescription?.Properties.FirstOrDefault( p => p.Name == nameof( SpeakerWireTool.Sound ) );
		var dropdownAttribute = soundProperty?.Attributes.OfType<DropdownPropertyAttribute>().FirstOrDefault();

		if ( dropdownAttribute == null || !dropdownAttribute.Options.Contains( data.Sound ) )
		{
			return ConstructDataValidationResult.Failure( $"Sound '{data.Sound}' is not in the approved list" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( SpeakerWireData data, Vector3 position, Rotation rotation )
	{
		// Create the speaker wire construct via code, not prefab
		var gameObject = new GameObject( true, "Speaker" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		// Add the main SpeakerWire component
		gameObject.Components.Create<SpeakerWire>();

		var model = Model.Load( "models/wire/speaker/speaker.vmdl" );

		// Add a basic model for visualization
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
