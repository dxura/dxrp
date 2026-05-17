using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.user.name", "#tool.wire.user.description", "#tool.group.interaction", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class UserWireTool() : BaseConstructTool<UserWireData>( ConstructType.UserWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Range" )]
	[Description( "Maximum range of the laser in units" )]
	[Range( UserWireDefinition.MinUserLaserWireRange, UserWireDefinition.MaxUserLaserWireRange )]
	public float Range
	{
		get => Data.Range;
		set => Data = Data with
		{
			Range = Math.Clamp( value, UserWireDefinition.MinUserLaserWireRange, UserWireDefinition.MaxUserLaserWireRange )
		};
	}
}
