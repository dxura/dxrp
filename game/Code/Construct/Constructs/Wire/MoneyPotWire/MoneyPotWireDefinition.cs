namespace Dxura.RP.Game.Wire;

public class MoneyPotWireDefinition : WireConstructDefinition<MoneyPotWire, MoneyPotWireData>
{
	public override ConstructType Type => ConstructType.MoneyPotWire;
	public override uint Limit => Config.Current.Game.MoneyPotWireLimit;

	protected override ConstructDataValidationResult ValidateWireTyped( MoneyPotWireData data )
	{
		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( MoneyPotWireData data, Vector3 position, Rotation rotation )
	{
		return GameObject.GetPrefab( "prefabs/constructs/wire/money_pot.prefab" ).Clone( position, rotation );
	}
}
