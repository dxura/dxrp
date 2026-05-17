namespace Dxura.RP.Game.Wire;

public record UserWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public float Range { get; set; } = UserWireDefinition.DefaultUserLaserWireRange;
}
