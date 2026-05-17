using Dxura.RP.Game.UI;
using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.speaker.name", "#tool.wire.speaker.description", "#tool.group.notify", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class SpeakerWireTool() : BaseConstructTool<SpeakerWireData>( ConstructType.SpeakerWire )
{
	public const float MinSpeakerVolume = 0.1f;
	public const float MaxSpeakerVolume = 1f;
	public const float MinSpeakerPitch = 0.1f;
	public const float MaxSpeakerPitch = 3.0f;
	public const float MinSpeakerDistance = 100f;
	public const float MaxSpeakerDistance = 1000f;

	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( 0, 180, 0 );

	[Property] [DropdownProperty(
		"sounds/beep.mp3",
		"sounds/purchase.mp3",
		"sounds/speaker/siren1.mp3",
		"sounds/speaker/siren2.mp3",
		"sounds/speaker/police_siren.mp3",
		"sounds/speaker/bell1.mp3",
		"sounds/speaker/bell2.mp3",
		"sounds/speaker/techno_loop_1.mp3",
		"sounds/speaker/techno_loop_2.mp3",
		"sounds/speaker/gamble_win.mp3",
		"sounds/speaker/gamble_lose.mp3",
		"sounds/speaker/gamble_spin.mp3",
		"sounds/speaker/instrument_organ.mp3",
		"sounds/speaker/instrument_trumpet.mp3",
		"sounds/speaker/instrument_piano.mp3",
		"sounds/speaker/instrument_xylophone.mp3",
		"sounds/speaker/dxrp_beat_1.mp3",
		"sounds/speaker/dxrp_beat_2.mp3",
		"sounds/speaker/bells.mp3"
	)]
	[Title( "Sound" )]
	public string Sound
	{
		get => Data.Sound;
		set => Data = Data with
		{
			Sound = value
		};
	}

	[Property]
	[Title( "Volume" )]
	[Range( MinSpeakerVolume, MaxSpeakerVolume )]
	public float Volume
	{
		get => Data.Volume;
		set => Data = Data with
		{
			Volume = value
		};
	}

	[Property]
	[Title( "Pitch" )]
	[Range( MinSpeakerPitch, MaxSpeakerPitch )]
	public float Pitch
	{
		get => Data.Pitch;
		set => Data = Data with
		{
			Pitch = value
		};
	}

	[Property]
	[Title( "Distance" )]
	[Description( "How far the sound will travel" )]
	[Range( MinSpeakerDistance, MaxSpeakerDistance )]
	public float Distance
	{
		get => Data.Distance;
		set => Data = Data with
		{
			Distance = value
		};
	}

	[Property]
	[Title( "Loop" )]
	[Description( "Whether the sound should loop continuously" )]
	public bool Loop
	{
		get => Data.Loop;
		set => Data = Data with
		{
			Loop = value
		};
	}
}
