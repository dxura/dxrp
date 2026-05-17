namespace Dxura.RP.Game;

public enum LightType
{
	Point,
	Spot
}

public record LightData : IConstructData
{
	public uint SchemaVersion => 1;

	public LightType Type { get; set; } = LightType.Point;

	public float Attenuation { get; set; } = LightDefinition.DefaultLightAttenuation;
	public float Radius { get; set; } = LightDefinition.DefaultLightRadius;
	public Color Color { get; set; } = Color.White;
	public float Cone { get; set; } = LightDefinition.DefaultLightCone;
}
