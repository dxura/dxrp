namespace Dxura.RP.Game.Wire;

public record IntervalWireData : IConstructData, IWireLabelData
{
	public uint SchemaVersion => 1;
	public float Interval { get; set; } = 1f;
	public float Hold { get; set; } = IntervalWireDefinition.MinIntervalWireHold;
	public string Label { get; set; } = string.Empty;
}
