using Dxura.RP.Game.Wire;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.keypad.name", "#tool.wire.keypad.description", "#tool.group.io", Category = ToolCategory.Wire )]
public class KeypadWireTool() : BaseConstructTool<KeypadWireData>( ConstructType.KeypadWire )
{
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
	[Title( "Toggle" )]
	[Description( "Toggles the signal (Continous On/Off Value" )]
	public bool Toggle
	{
		get => Data.Toggle;
		set => Data = Data with
		{
			Toggle = value
		};
	}
}
