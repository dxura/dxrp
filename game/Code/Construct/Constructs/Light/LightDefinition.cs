using Dxura.RP.Shared;
namespace Dxura.RP.Game;

public class LightDefinition : ConstructDefinition<Light, LightData>
{
	public override ConstructType Type => ConstructType.Light;
	public override uint Limit => Config.Current.Game.LightLimit;

	public const float MinLightAttenuation = 0.1f;
	public const float MaxLightAttenuation = 10.0f;
	public const float DefaultLightAttenuation = 1.0f;
	public const float MinLightRadius = 50f;
	public const float MaxLightRadius = 800f;
	public const float DefaultLightRadius = 500f;
	public const float MinLightCone = 2f;
	public const float MaxLightCone = 90f;
	public const float DefaultLightCone = 45f;

	protected override ConstructDataValidationResult ValidateTyped( LightData data )
	{
		if ( data.Attenuation is < MinLightAttenuation or > MaxLightAttenuation )
		{
			return ConstructDataValidationResult.Failure(
				$"Light attenuation must be between {MinLightAttenuation} and {MaxLightAttenuation}" );
		}

		if ( data.Radius is < MinLightRadius or > MaxLightRadius )
		{
			return ConstructDataValidationResult.Failure(
				$"Light radius must be between {MinLightRadius} and {MaxLightRadius}" );
		}

		if ( data.Cone is < MinLightCone or > MaxLightCone )
		{
			return ConstructDataValidationResult.Failure(
				$"Light cone must be between {MinLightCone} and {MaxLightCone}" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( LightData data, Vector3 position, Rotation rotation )
	{
		var lightGameObject = GameObject.GetPrefab( "prefabs/constructs/light.prefab" ).Clone( position, rotation );

		return lightGameObject;
	}
}
