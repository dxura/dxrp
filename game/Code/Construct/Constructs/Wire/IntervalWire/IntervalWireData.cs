namespace Dxura.RP.Game.Wire;

public record IntervalWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public float Interval { get; set; } = 1f;
	public float Hold { get; set; } = IntervalWireDefinition.MinIntervalWireHold;
}
