namespace Dxura.RP.Game.Wire;

[Title( "Interval" )]
[Category( "Wire" )]
[Icon( "cable" )]
public class IntervalWire() : BaseWireConstruct( ConstructType.IntervalWire ), IWireEvents
{
	private IntervalWireData _data = new();

	[WireOutput( "signal" )]
	private bool Signal { get; set; }

	[WireInput( "halt" )]
	private bool Halt { get; set; }

	public override string Name => $"Interval ({_data.Interval:0.##}s{(_data.Hold > 0 ? $", {_data.Hold:0.##}s hold" : "")})";

	private TimeUntil _nextPulse;

	public void OnWireTick()
	{
		if ( Halt )
		{
			Signal = false;
			_nextPulse = _data.Interval;
			return;
		}

		if ( !_nextPulse )
		{
			return;
		}

		if ( Signal )
		{
			// End of hold period, turn off signal
			Signal = false;
			_nextPulse = _data.Interval;
		}
		else
		{
			// Start of pulse, turn on signal (and start hold timer if needed)
			Signal = true;
			_nextPulse = _data.Hold > 0 ? _data.Hold : 0f;
		}
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as IntervalWireData ?? new IntervalWireData();
	}
}
