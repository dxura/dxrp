namespace Dxura.RP.Game.Wire;

public class KeypadWireDefinition : ConstructDefinition<KeypadWire, KeypadWireData>
{
	public override ConstructType Type => ConstructType.KeypadWire;
	public override uint Limit => Config.Current.Game.KeypadWireLimit;

	protected override ConstructDataValidationResult ValidateTyped( KeypadWireData data )
	{
		if ( data.OffValue is < ButtonWireDefinition.MinButtonValue or > ButtonWireDefinition.MaxButtonValue )
		{
			return ConstructDataValidationResult.Failure( $"Off Value must be between {ButtonWireDefinition.MinButtonValue} and {ButtonWireDefinition.MaxButtonValue}" );
		}

		if ( data.OnValue is < ButtonWireDefinition.MinButtonValue or > ButtonWireDefinition.MaxButtonValue )
		{
			return ConstructDataValidationResult.Failure( $"On Value must be between {ButtonWireDefinition.MinButtonValue} and {ButtonWireDefinition.MaxButtonValue}" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( KeypadWireData data, Vector3 position, Rotation rotation )
	{
		return GameObject.GetPrefab( "prefabs/constructs/wire/keypad.prefab" ).Clone( position, rotation );
	}
}
