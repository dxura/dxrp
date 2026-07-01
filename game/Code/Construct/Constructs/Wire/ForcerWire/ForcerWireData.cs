namespace Dxura.RP.Game.Wire;

public record ForcerWireData : IConstructData, IWireLabelData
{
	public uint SchemaVersion => 1;
	public float ForceAmount { get; set; } = 100f;
	public float Range { get; set; } = ForcerWireDefinition.DefaultForcerLaserWireRange;
	public bool Uniform { get; set; } = false;
	public string Label { get; set; } = string.Empty;
}
