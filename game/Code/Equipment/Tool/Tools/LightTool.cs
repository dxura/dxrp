namespace Dxura.RP.Game.Tools;

[Tool( "#tool.light.name", "#tool.light.description", "#tool.group.construction" )]
public class LightTool() : BaseConstructTool<LightData>( ConstructType.Light )
{
	[Property]
	[Title( "Type" )]
	public LightType Type
	{
		get => Data.Type;
		set => Data = Data with
		{
			Type = value
		};
	}

	[Property]
	[Title( "Attenuation" )]
	[Description( "Amount of reduction in the intensity of light as it travels" )]
	[Range( LightDefinition.MinLightAttenuation, LightDefinition.MaxLightAttenuation )]
	public float Attenuation
	{
		get => Data.Attenuation;
		set => Data = Data with
		{
			Attenuation = value
		};
	}

	[Property]
	[Title( "Radius" )]
	[Range( LightDefinition.MinLightRadius, LightDefinition.MaxLightRadius )]
	public float Radius
	{
		get => Data.Radius;
		set => Data = Data with
		{
			Radius = value
		};
	}

	[Property]
	[Title( "Color" )]
	public Color Color
	{
		get => Data.Color;
		set => Data = Data with
		{
			Color = value
		};
	}


	[Property]
	[Title( "Cone" )]
	[Description( "Cone angle for spotlight" )]
	[Range( LightDefinition.MinLightCone, LightDefinition.MaxLightCone )]
	public float Cone
	{
		get => Data.Cone;
		set => Data = Data with
		{
			Cone = value
		};
	}
}
