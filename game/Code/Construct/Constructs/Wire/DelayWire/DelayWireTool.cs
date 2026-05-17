using Dxura.RP.Game.UI;
using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.delay.name", "#tool.wire.delay.description", "#tool.group.logic", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class DelayWireTool() : BaseConstructTool<DelayWireData>( ConstructType.DelayWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Delay" )]
	[Description( "How long to delay (in seconds)" )]
	[Range( DelayWireDefinition.MinDelayWireDelay, DelayWireDefinition.MaxDelayWireDelay )]
	[Step( 1 )]
	public int Interval
	{
		get => Data.Delay;
		set => Data = Data with
		{
			Delay = value
		};
	}

}
