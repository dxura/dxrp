namespace Dxura.RP.Game.Wire;

public record DelayWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public int Delay { get; set; } = DelayWireDefinition.MinDelayWireDelay;
}
