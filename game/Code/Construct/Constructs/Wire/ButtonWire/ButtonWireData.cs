namespace Dxura.RP.Game.Wire;

public record ButtonWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public bool Toggle { get; set; }
	public float OffValue { get; set; } = ButtonWireDefinition.DefaultButtonOffValue;
	public float OnValue { get; set; } = ButtonWireDefinition.DefaultButtonOnValue;
	public string Label { get; set; } = string.Empty;
}
