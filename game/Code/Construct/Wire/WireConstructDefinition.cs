namespace Dxura.RP.Game.Wire;

public abstract class WireConstructDefinition<TConstruct, TData> : ConstructDefinition<TConstruct, TData>
	where TConstruct : BaseWireConstruct
	where TData : IConstructData, IWireLabelData, new()
{
	protected override ConstructDataValidationResult ValidateTyped( TData data )
	{
		var labelResult = WireLabelHelper.ValidateLabel( data.Label );
		if ( !labelResult.IsValid )
		{
			return labelResult;
		}

		return ValidateWireTyped( data );
	}

	protected virtual ConstructDataValidationResult ValidateWireTyped( TData data )
	{
		return ConstructDataValidationResult.Success();
	}
}
