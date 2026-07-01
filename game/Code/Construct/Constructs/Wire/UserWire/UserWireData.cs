namespace Dxura.RP.Game.Wire;

public record UserWireData : IConstructData, IWireLabelData
{
	public uint SchemaVersion => 1;
	public float Range { get; set; } = UserWireDefinition.DefaultUserLaserWireRange;
	public string Label { get; set; } = string.Empty;
}
