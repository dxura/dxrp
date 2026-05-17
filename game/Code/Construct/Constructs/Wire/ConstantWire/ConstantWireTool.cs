using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.constant.name", "#tool.wire.constant.description", "#tool.group.core", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class ConstantWireTool() : BaseConstructTool<ConstantWireData>( ConstructType.ConstantWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Type" )]
	[Description( "The wire type for this constant" )]
	public ConstantWireType Type
	{
		get => Data.Type;
		set => Data = Data with
		{
			Type = value
		};
	}

	[Property]
	[Title( "Number Value" )]
	[Range( ButtonWireDefinition.MinButtonValue, ButtonWireDefinition.MaxButtonValue )]
	public float NumberValue
	{
		get => Data.FloatValue;
		set => Data = Data with
		{
			FloatValue = value
		};
	}

	[Property]
	[Title( "Bool Value" )]
	public bool BoolValue
	{
		get => Data.BoolValue;
		set => Data = Data with
		{
			BoolValue = value
		};
	}

	[Property]
	[Title( "String Value" )]
	public string StringValue
	{
		get => Data.StringValue;
		set => Data = Data with
		{
			StringValue = value ?? ""
		};
	}

}
