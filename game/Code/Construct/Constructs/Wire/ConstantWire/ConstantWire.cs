namespace Dxura.RP.Game.Wire;

[Title( "Constant" )]
[Category( "Wire" )]
[Icon( "fiber_manual_record" )]
public class ConstantWire() : BaseWireConstruct( ConstructType.ConstantWire )
{
	private ConstantWireData _data = new();

	public override string Name => $"Constant ({_data.Type}: {GetValueString()})";

	protected override void OnStart()
	{
		if ( IsPreview )
		{
			return;
		}

		// Initialize output port with selected type
		RegisterOutputPort( "value", _data.WireType );

		// Initialize the chain, and set initial data
		base.OnStart();

		Wire.Current.SetOutputValue( this, "value", _data.Value );
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as ConstantWireData ?? new ConstantWireData();

		Wire.Current.SetOutputValue( this, "value", _data.Value );
	}

	private string GetValueString()
	{
		return _data.Type switch
		{
			ConstantWireType.Number => _data.FloatValue.ToString( "F2" ),
			ConstantWireType.Bool => _data.BoolValue.ToString(),
			ConstantWireType.String => $"\"{_data.StringValue}\"",
			_ => "0"
		};
	}
}
