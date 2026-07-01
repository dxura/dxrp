namespace Dxura.RP.Game.Wire;

public record DelayWireData : IConstructData, IWireLabelData
{
	public uint SchemaVersion => 1;
	public int Delay { get; set; } = DelayWireDefinition.MinDelayWireDelay;
	public string Label { get; set; } = string.Empty;
}
