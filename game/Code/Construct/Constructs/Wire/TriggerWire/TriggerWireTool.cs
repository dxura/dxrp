using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.trigger.name", "#tool.wire.trigger.description", "#tool.group.sensor", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class TriggerWireTool() : BaseConstructTool<TriggerWireData>( ConstructType.TriggerWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Range" )]
	[Description( "Maximum range of the trigger line in units" )]
	[Range( TriggerWireDefinition.MinTriggerLaserWireRange, TriggerWireDefinition.MaxTriggerLaserWireRange )]
	public float Range
	{
		get => Data.Range;
		set => Data = Data with
		{
			Range = Math.Clamp( value, TriggerWireDefinition.MinTriggerLaserWireRange, TriggerWireDefinition.MaxTriggerLaserWireRange )
		};
	}

	[Property]
	[Title( "Filter" )]
	[Description( "What types of objects should trigger this wire" )]
	public TriggerFilterType FilterType
	{
		get => Data.FilterType;
		set => Data = Data with
		{
			FilterType = value
		};
	}

	[Property]
	[Title( "Info Source" )]
	[Description( "What information to provide (when applicable)" )]
	public TriggerInfoSource InfoSource
	{
		get => Data.InfoSource;
		set => Data = Data with
		{
			InfoSource = value
		};
	}
}
