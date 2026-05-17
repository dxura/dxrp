namespace Dxura.RP.Game.Wire;

public record SpeakerWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public string Sound { get; set; } = "sounds/beep.mp3";
	public float Volume { get; set; } = SpeakerWireDefinition.DefaultSpeakerVolume;
	public float Pitch { get; set; } = SpeakerWireDefinition.DefaultSpeakerPitch;
	public float Distance { get; set; } = SpeakerWireDefinition.DefaultSpeakerDistance;
	public bool Loop { get; set; } = false;
}
