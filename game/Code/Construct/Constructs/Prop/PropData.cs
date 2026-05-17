namespace Dxura.RP.Game;

public struct PropData() : IConstructData
{
	public readonly uint SchemaVersion => 1;

	public string Model { get; set; } = string.Empty;
	public string? Material { get; set; } = null;

	public Color? Tint { get; set; } = null;
	public bool NoCollide { get; set; } = false;

	public float? Friction { get; set; } = null;
	public float? Elasticity { get; set; } = null;

	public bool FadingDoor { get; set; } = false;
	public float? FadingDoorDuration { get; set; } = null;
	public bool FadingDoorIsReversed { get; set; } = false;

	public Vector3 Scale { get; set; } = Vector3.One;
}
