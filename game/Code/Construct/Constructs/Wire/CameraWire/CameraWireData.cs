namespace Dxura.RP.Game.Wire;

public record CameraWireData : IConstructData
{
	public uint SchemaVersion => 1;

	public string Identifier { get; set; } = Guid.NewGuid().ToString();
}
