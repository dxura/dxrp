using Dxura.RP.Game.Wire;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.synthesizer.name", "#tool.wire.synthesizer.description", "#tool.group.notify", Category = ToolCategory.Wire, MinimumLevel = 2 )]
public class SynthesizerWireTool() : BaseConstructTool<SynthesizerWireData>( ConstructType.SynthesizerWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( 0, 180, 0 );

	[Property]
	[Title( "Volume" )]
	[Range( 0f, 1f )]
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
	[Range( SpeakerWireTool.MinSpeakerPitch, SpeakerWireTool.MaxSpeakerPitch )]
	public float Pitch
	{
		get => Data.Pitch;
		set => Data = Data with
		{
			Pitch = value
		};
	}

	[Property]
	[Title( "Auto Play" )]
	[Description( "Automatically play when text is received" )]
	public bool AutoPlay
	{
		get => Data.AutoPlay;
		set => Data = Data with
		{
			AutoPlay = value
		};
	}
}
