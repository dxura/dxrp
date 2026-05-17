using Dxura.RP.Game.UI;
using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.led.name", "#tool.wire.led.description", "#tool.group.notify", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class LedWireTool() : BaseConstructTool<LedWireData>( ConstructType.LedWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Off Color" )]
	[Description( "Color when the LED is off" )]
	public Color OffColor
	{
		get => Data.OffColor;
		set => Data = Data with
		{
			OffColor = value
		};
	}

	[Property]
	[Title( "On Color" )]
	[Description( "Color when the LED is on" )]
	public Color OnColor
	{
		get => Data.OnColor;
		set => Data = Data with
		{
			OnColor = value
		};
	}
}
