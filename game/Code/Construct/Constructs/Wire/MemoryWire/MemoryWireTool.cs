using Dxura.RP.Game.UI;
using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.memory.name", "#tool.wire.memory.description", "#tool.group.logic", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class MemoryWireTool() : BaseConstructTool<MemoryWireData>( ConstructType.MemoryWire );
