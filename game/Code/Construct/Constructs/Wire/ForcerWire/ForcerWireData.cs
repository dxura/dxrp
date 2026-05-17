namespace Dxura.RP.Game.Wire;

public record ForcerWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public float ForceAmount { get; set; } = 100f;
	public float Range { get; set; } = ForcerWireDefinition.DefaultForcerLaserWireRange;
	public bool Uniform { get; set; } = false;
}
