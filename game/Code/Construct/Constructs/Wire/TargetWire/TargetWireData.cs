namespace Dxura.RP.Game.Wire;

public record TargetWireData : IConstructData, IWireLabelData
{
	public uint SchemaVersion => 1;
	public string Label { get; set; } = string.Empty;
}
