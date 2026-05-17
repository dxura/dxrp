using Dxura.RP.Game.UI;
using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.interval.name", "#tool.wire.interval.description", "#tool.group.logic", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class IntervalWireTool() : BaseConstructTool<IntervalWireData>( ConstructType.IntervalWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Interval" )]
	[Description( "How often to pulse (in seconds)" )]
	[Range( IntervalWireDefinition.MinIntervalWireInterval, IntervalWireDefinition.MaxIntervalWireInterval )]
	[Step( 0.1f )]
	public float Interval
	{
		get => Data.Interval;
		set => Data = Data with
		{
			Interval = value
		};
	}

	[Property]
	[Title( "Hold" )]
	[Description( "How long to hold the signal (in seconds)" )]
	[Range( IntervalWireDefinition.MinIntervalWireHold, IntervalWireDefinition.MaxIntervalWireHold )]
	[Step( 0.1f )]
	public float Hold
	{
		get => Data.Hold;
		set => Data = Data with
		{
			Hold = value
		};
	}
}
