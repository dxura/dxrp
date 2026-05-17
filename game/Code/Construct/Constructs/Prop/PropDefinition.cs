namespace Dxura.RP.Game;

public class PropDefinition() : ConstructDefinition<Prop, PropData>
{
	public override ConstructType Type => ConstructType.Prop;
	public override uint Limit => Config.Current.Game.PropLimit;

	public const float MinPropScale = 0.3f;
	public const float MaxPropScale = 2f;
	public const float MinFadingDoorDuration = 1.5f;
	public const float MaxFadingDoorDuration = 60f;
	public const float MinPropFriction = 0.02f;
	public const float MaxPropFriction = 2.5f;
	public const float MinPropElasticity = 0.1f;
	public const float MaxPropElasticity = 0.80f;


	protected override ConstructDataValidationResult ValidateTyped( PropData data )
	{
		// Validate model path
		if ( string.IsNullOrWhiteSpace( data.Model ) )
		{
			return ConstructDataValidationResult.Failure( "Model path cannot be empty" );
		}

		// Validate scale bounds
		if ( data.Scale.x < MinPropScale || data.Scale.x > MaxPropScale ||
		     data.Scale.y < MinPropScale || data.Scale.y > MaxPropScale ||
		     data.Scale.z < MinPropScale || data.Scale.z > MaxPropScale )
		{
			return ConstructDataValidationResult.Failure( $"Scale values must be between {MinPropScale} and {MaxPropScale}" );
		}

		// Validate color values are reasonable (no NaN, etc.)
		if ( data.Tint.HasValue )
		{
			var tint = data.Tint.Value;
			if ( float.IsNaN( tint.r ) || float.IsNaN( tint.g ) || float.IsNaN( tint.b ) || float.IsNaN( tint.a ) )
			{
				return ConstructDataValidationResult.Failure( "Invalid color values" );
			}

			if ( tint.a < 1f )
			{
				return ConstructDataValidationResult.Failure( "Color alpha must be 1 (fully opaque)" );
			}
		}

		// Validate physics properties
		if ( data.Friction.HasValue )
		{
			var friction = data.Friction.Value;
			if ( float.IsNaN( friction ) || friction < MinPropFriction || friction > MaxPropFriction )
			{
				return ConstructDataValidationResult.Failure( $"Friction must be between {MinPropFriction} and {MaxPropFriction}" );
			}
		}

		if ( data.Elasticity.HasValue )
		{
			var elasticity = data.Elasticity.Value;
			if ( float.IsNaN( elasticity ) || elasticity < MinPropElasticity || elasticity > MaxPropElasticity )
			{
				return ConstructDataValidationResult.Failure( $"Elasticity must be between {MinPropElasticity} and {MaxPropElasticity}" );
			}
		}

		// Validate fading door properties
		if ( data.FadingDoor && data.FadingDoorDuration.HasValue )
		{
			var duration = data.FadingDoorDuration.Value;

			// Allow 0 for switch state mode or validate within proper range
			if ( duration != 0f && duration is < MinFadingDoorDuration or > MaxFadingDoorDuration )
			{
				return ConstructDataValidationResult.Failure( $"Fading door duration must be 0 (switch mode) or between {MinFadingDoorDuration}s and {MaxFadingDoorDuration}s" );
			}

			// Validate duration is not NaN or negative (except 0)
			if ( float.IsNaN( duration ) || duration < 0f )
			{
				return ConstructDataValidationResult.Failure( "Fading door duration must be a valid positive number or 0" );
			}
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( PropData data, Vector3 position, Rotation rotation )
	{
		var propGameObject = GameObject.GetPrefab( "prefabs/constructs/prop.prefab" ).Clone( position, rotation );

		return propGameObject;
	}
}
