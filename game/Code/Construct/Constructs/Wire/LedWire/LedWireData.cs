namespace Dxura.RP.Game.Wire;

public record LedWireData : IConstructData, IWireLabelData
{
	public uint SchemaVersion => 1;
	public Color OffColor { get; set; } = Color.Red;
	public Color OnColor { get; set; } = Color.Green;
	public string Label { get; set; } = string.Empty;
}
