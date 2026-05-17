using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.camera.name", "#tool.wire.camera.description", "#tool.group.sensor", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class CameraWireTool() : BaseConstructTool<CameraWireData>( ConstructType.CameraWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Identifier" )]
	[Description( "Identity of this camera used to link with screens" )]
	public string Identifier
	{
		get => Data.Identifier;
		set => Data = Data with
		{
			Identifier = value
		};
	}

	public override void PrimaryUseEnd()
	{
		base.PrimaryUseEnd();

		// Cycle the data (If not overrode)
		Data.Identifier = Guid.NewGuid().ToString();
	}
}
