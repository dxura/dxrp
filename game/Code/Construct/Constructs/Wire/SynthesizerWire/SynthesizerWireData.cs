namespace Dxura.RP.Game.Wire;

public record SynthesizerWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public float Volume { get; set; } = 1.0f;
	public float Pitch { get; set; } = SpeakerWireDefinition.DefaultSpeakerPitch;
	public bool AutoPlay { get; set; } = true;
}
