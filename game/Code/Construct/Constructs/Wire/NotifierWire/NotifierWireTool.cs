using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.notifier.name", "#tool.wire.notifier.description", "#tool.group.notify", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class NotifierWireTool() : BaseConstructTool<NotifierWireData>( ConstructType.NotifierWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Message" )]
	[Range( NotifierWireDefinition.MinNotifierTextLength, NotifierWireDefinition.MaxNotifierTextLength )]
	[Description( "The notification message to display" )]
	public string Message
	{
		get => Data.Message;
		set => Data = Data with
		{
			Message = value
		};
	}

	[Property]
	[Title( "Include Value" )]
	[Description( "Should the value provided be shown." )]
	public bool IncludeValue
	{
		get => Data.IncludeValue;
		set => Data = Data with
		{
			IncludeValue = value
		};
	}

	[Property]
	[Title( "Ignore Falsy Value" )]
	[Description( "Should we ignore values which are considered false (0)" )]
	public bool IgnoreFalsyValue
	{
		get => Data.IgnoreFalsyValue;
		set => Data = Data with
		{
			IgnoreFalsyValue = value
		};
	}
}
