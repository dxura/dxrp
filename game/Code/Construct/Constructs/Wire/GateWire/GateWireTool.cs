using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.gate.name", "#tool.wire.gate.description", "#tool.group.logic", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class GateWireTool() : BaseConstructTool<GateWireData>( ConstructType.GateWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Gate Type" )]
	[Description( "The type of logic gate to create" )]
	public GateType GateType
	{
		get => Data.Type;
		set => Data = Data with
		{
			Type = value
		};
	}
}
