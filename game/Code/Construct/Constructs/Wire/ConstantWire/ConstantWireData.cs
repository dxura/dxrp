using System.Text.Json.Serialization;
namespace Dxura.RP.Game.Wire;

public record ConstantWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public ConstantWireType Type { get; set; } = ConstantWireType.Number;
	public float FloatValue { get; set; } = ConstantWireDefinition.DefaultConstantFloat;
	public int IntValue { get; set; } = ConstantWireDefinition.DefaultConstantInt;
	public bool BoolValue { get; set; } = false;
	public string StringValue { get; set; } = "";

	[JsonIgnore]
	public object Value => Type switch
	{
		ConstantWireType.Number => FloatValue,
		ConstantWireType.Bool => BoolValue,
		ConstantWireType.String => StringValue,
		_ => FloatValue
	};

	[JsonIgnore]
	public WireType WireType => Type switch
	{
		ConstantWireType.Number => WireType.Number,
		ConstantWireType.Bool => WireType.Bool,
		ConstantWireType.String => WireType.String,
		_ => WireType.Number
	};
}
