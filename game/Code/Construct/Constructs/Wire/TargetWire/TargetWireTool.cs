using Dxura.RP.Game.Wire;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.target.name", "#tool.wire.target.description", "#tool.group.interaction", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class TargetWireTool() : BaseConstructTool<TargetWireData>( ConstructType.TargetWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

}
