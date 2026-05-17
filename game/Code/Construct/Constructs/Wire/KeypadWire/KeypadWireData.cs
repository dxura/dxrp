namespace Dxura.RP.Game.Wire;

public record KeypadWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public float OffValue { get; set; } = ButtonWireDefinition.DefaultButtonOffValue;
	public float OnValue { get; set; } = ButtonWireDefinition.DefaultButtonOnValue;

	public bool Toggle { get; set; }
}
