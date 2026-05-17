using Dxura.RP.Game.UI;
using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.button.name", "#tool.wire.button.description", "#tool.group.io", Category = ToolCategory.Wire )]
public class ButtonWireTool() : BaseConstructTool<ButtonWireData>( ConstructType.ButtonWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Toggle Mode" )]
	[Description( "When enabled, button stays pressed until pressed again" )]
	public bool Toggle
	{
		get => Data.Toggle;
		set => Data = Data with
		{
			Toggle = value
		};
	}

	[Property]
	[Title( "Off Value" )]
	[Description( "Output value when button is not pressed" )]
	[Range( ButtonWireDefinition.MinButtonValue, ButtonWireDefinition.MaxButtonValue )]
	public float OffValue
	{
		get => Data.OffValue;
		set => Data = Data with
		{
			OffValue = value
		};
	}

	[Property]
	[Title( "On Value" )]
	[Description( "Output value when button is pressed" )]
	[Range( ButtonWireDefinition.MinButtonValue, ButtonWireDefinition.MaxButtonValue )]
	public float OnValue
	{
		get => Data.OnValue;
		set => Data = Data with
		{
			OnValue = value
		};
	}

	[Property]
	[Title( "Label" )]
	[Description( "Describe this button (optional)" )]
	[Range( TextDefinition.MinTextLength, TextDefinition.MaxTextLength )]
	public string Label
	{
		get => Data.Label;
		set => Data = Data with
		{
			Label = value
		};
	}
}
