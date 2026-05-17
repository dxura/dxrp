namespace Dxura.RP.Game.Wire;

public class CameraWireDefinition : ConstructDefinition<CameraWire, CameraWireData>
{
	public override ConstructType Type => ConstructType.CameraWire;
	public override uint Limit => Config.Current.Game.CameraWireLimit;

	public const string CameraPrefix = "camera:";

	protected override ConstructDataValidationResult ValidateTyped( CameraWireData data )
	{
		// if ( data.Delay is < GameConfig.MinDelayWireDelay or > GameConfig.MaxDelayWireDelay )
		// {
		// 	return ConstructDataValidationResult.Failure( $"Delay must be between {GameConfig.MinDelayWireDelay} and {GameConfig.MaxDelayWireDelay} seconds" );
		// }

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( CameraWireData data, Vector3 position, Rotation rotation )
	{
		return GameObject.GetPrefab( "prefabs/constructs/wire/camera.prefab" ).Clone( position, rotation );

	}
}
