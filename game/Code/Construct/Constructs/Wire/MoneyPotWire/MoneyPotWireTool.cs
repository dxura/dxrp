using Dxura.RP.Game.UI;
using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.moneypot.name", "#tool.wire.moneypot.description", "#tool.group.interaction", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class MoneyPotWireTool() : BaseConstructTool<MoneyPotWireData>( ConstructType.MoneyPotWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

}
