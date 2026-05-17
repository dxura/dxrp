namespace Dxura.RP.Game.Wire;

public record LedWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public Color OffColor { get; set; } = Color.Red;
	public Color OnColor { get; set; } = Color.Green;
}
