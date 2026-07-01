namespace Dxura.RP.Game.Wire;

public record CameraWireData : IConstructData, IWireLabelData
{
	public uint SchemaVersion => 1;

	public string Identifier { get; set; } = Guid.NewGuid().ToString();
	public string Label { get; set; } = string.Empty;
}
