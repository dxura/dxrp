namespace Dxura.RP.Game.Wire;

[Title( "Memory" )]
[Category( "Wire" )]
[Icon( "cable" )]
public class MemoryWire() : BaseWireConstruct( ConstructType.MemoryWire ), IWireEvents
{
	private MemoryWireData _data = new();
	private object? _storedValue;
	private bool _lastStoreState;
	private bool _lastClearState;

	[WireInput( "input" )]
	private object? Input { get; set; }

	[WireInput( "store" )]
	private bool Store { get; set; }

	[WireInput( "clear" )]
	private bool Clear { get; set; }

	[WireOutput( "output" )]
	private object? Output { get; set; }

	[WireOutput( "has_value" )]
	private bool HasValue { get; set; }

	public override string Name => $"Memory";

	public void OnWireTick()
	{
		// Edge detection for clear (takes priority)
		if ( Clear && !_lastClearState )
		{
			_storedValue = null;
			UpdateOutputs();
		}
		// Edge detection for store
		else if ( Store && !_lastStoreState )
		{
			_storedValue = Input;
			UpdateOutputs();
		}

		_lastStoreState = Store;
		_lastClearState = Clear;
	}

	private void UpdateOutputs()
	{
		Output = _storedValue;
		HasValue = _storedValue != null;
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as MemoryWireData ?? new MemoryWireData();
		UpdateOutputs();
	}
}
