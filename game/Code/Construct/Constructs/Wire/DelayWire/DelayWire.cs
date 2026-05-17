namespace Dxura.RP.Game.Wire;

[Title( "Delay" )]
[Category( "Wire" )]
[Icon( "cable" )]
public class DelayWire() : BaseWireConstruct( ConstructType.DelayWire ), IWireEvents
{
	private DelayWireData _data = new();

	[WireOutput( "output" )]
	private object? Output { get; set; }

	[WireInput( "input" )]
	private object? Input
	{
		set
		{
			_delayQueue.Enqueue( (value, _data.Delay) );

			// Safeguard: limit queue size to prevent memory issues
			while ( _delayQueue.Count > 100 )
			{
				_delayQueue.Dequeue();
			}
		}
		get => false; // This is just a trigger, no need to store state
	}

	[WireInput( "clear" )]
	public bool Clear
	{
		set
		{
			if ( value )
			{
				_delayQueue.Clear();
			}
		}
		get => false; // This is just a trigger, no need to store state
	}

	public override string Name => $"Delay ({_data.Delay}s)";

	private readonly Queue<(object? value, TimeUntil outputTime)> _delayQueue = new();

	public void OnWireTick()
	{
		// Process any delayed signals that are due
		while ( _delayQueue.Count > 0 && _delayQueue.Peek().outputTime )
		{
			var (value, _) = _delayQueue.Dequeue();
			Output = value;
		}
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as DelayWireData ?? new DelayWireData();
	}
}
