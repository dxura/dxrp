namespace Dxura.RP.Game.Wire;

public record MemoryWireData : IConstructData, IWireLabelData
{
	public uint SchemaVersion => 1;
	public string Label { get; set; } = string.Empty;
}
