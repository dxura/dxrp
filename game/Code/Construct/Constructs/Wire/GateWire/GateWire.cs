namespace Dxura.RP.Game.Wire;

[Title( "Gate" )]
[Category( "Wire" )]
[Icon( "cable" )]
public class GateWire() : BaseWireConstruct( ConstructType.GateWire ), IWireEvents
{
	private GateWireData _data = new();

	[WireInput( "a" )]
	private object? InputA { get; set; }

	[WireInput( "b" )]
	private object? InputB { get; set; }

	[WireInput( "c" )]
	private object? InputC { get; set; }

	[WireInput( "d" )]
	private object? InputD { get; set; }

	[WireInput( "e" )]
	private object? InputE { get; set; }

	[WireInput( "process" )]
	private bool Process { get; set; }

	[WireOutput( "out" )]
	private object? Out { get; set; }

	public override string Name => $"Gate ({_data.Type})";

	private bool _inputALinked = false;
	private bool _inputBLinked = false;
	private bool _inputCLinked = false;
	private bool _inputDLinked = false;
	private bool _inputELinked = false;
	private bool _srLatchOutput = false;

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as GateWireData ?? new GateWireData();
	}

	protected override void OnStart()
	{
		base.OnStart();
		ProcessGate();
		InitializeInputLinkedState();
	}

	public override void OnWireInput( string inputId, WireValue value )
	{
		base.OnWireInput( inputId, value );

		// Mark this input as linked when it receives a value
		MarkInputAsLinked( inputId );

		// Skip processing only when the Process input itself becomes false
		// For A, B, C, D, E inputs, always process regardless of current Process state
		if ( inputId == "process" && !Convert.ToBoolean( value.Value ) )
		{
			return;
		}

		// Process immediately for instant response
		ProcessGate();
	}

	public override void OnWireInputDisconnected( string inputId )
	{
		// Mark this input as unlinked when the wire is disconnected
		MarkInputAsUnlinked( inputId );

		// Re-process the gate with the disconnected input
		ProcessGate();
	}

	private void ProcessGate()
	{
		// Convert inputs to appropriate types
		var numA = ConvertToFloat( InputA );
		var numB = ConvertToFloat( InputB );
		var numC = ConvertToFloat( InputC );
		var numD = ConvertToFloat( InputD );
		var numE = ConvertToFloat( InputE );

		var strA = InputA?.ToString() ?? "";
		var strB = InputB?.ToString() ?? "";
		var strC = InputC?.ToString() ?? "";
		var strD = InputD?.ToString() ?? "";
		var strE = InputE?.ToString() ?? "";

		var boolInputA = numA > GateWireDefinition.GateBooleanThreshold;
		var boolInputB = numB > GateWireDefinition.GateBooleanThreshold;
		var boolInputC = numC > GateWireDefinition.GateBooleanThreshold;
		var boolInputD = numD > GateWireDefinition.GateBooleanThreshold;
		var boolInputE = numE > GateWireDefinition.GateBooleanThreshold;

		Out = _data.Type switch
		{
			// Boolean logic gates
			GateType.And =>
				(!_inputALinked || boolInputA) &&
				(!_inputBLinked || boolInputB) &&
				(!_inputCLinked || boolInputC) &&
				(!_inputDLinked || boolInputD) &&
				(!_inputELinked || boolInputE)
					? GateWireDefinition.GateLogicHigh
					: GateWireDefinition.GateLogicLow,

			GateType.Or =>
				_inputALinked && boolInputA ||
				_inputBLinked && boolInputB ||
				_inputCLinked && boolInputC ||
				_inputDLinked && boolInputD ||
				_inputELinked && boolInputE
					? GateWireDefinition.GateLogicHigh
					: GateWireDefinition.GateLogicLow,

			GateType.Not => !boolInputA ? GateWireDefinition.GateLogicHigh : GateWireDefinition.GateLogicLow,

			GateType.Xor => boolInputA ^ boolInputB ? GateWireDefinition.GateLogicHigh : GateWireDefinition.GateLogicLow,

			GateType.Nand =>
				!((!_inputALinked || boolInputA) &&
				  (!_inputBLinked || boolInputB) &&
				  (!_inputCLinked || boolInputC) &&
				  (!_inputDLinked || boolInputD) &&
				  (!_inputELinked || boolInputE))
					? GateWireDefinition.GateLogicHigh
					: GateWireDefinition.GateLogicLow,

			GateType.Nor =>
				!(_inputALinked && boolInputA ||
				  _inputBLinked && boolInputB ||
				  _inputCLinked && boolInputC ||
				  _inputDLinked && boolInputD ||
				  _inputELinked && boolInputE)
					? GateWireDefinition.GateLogicHigh
					: GateWireDefinition.GateLogicLow,

			// Conditional operations
			GateType.If => boolInputA ? InputB : InputC,

			// Arithmetic operations
			GateType.Add => numA + numB + numC + numD + numE,
			GateType.Subtract => numA - numB - numC - numD - numE,
			GateType.Multiply => numA * numB,
			GateType.Divide => numB != GateWireDefinition.GateLogicLow ? numA / numB : GateWireDefinition.GateLogicLow,
			GateType.Modulo => numB != GateWireDefinition.GateLogicLow ? numA % numB : GateWireDefinition.GateLogicLow,
			GateType.Power => MathF.Pow( numA, numB ),

			// Comparison operations
			GateType.Min => MathF.Min( numA, numB ),
			GateType.Max => MathF.Max( numA, numB ),
			GateType.Equal => IsEqual( InputA, InputB ) ? GateWireDefinition.GateLogicHigh : GateWireDefinition.GateLogicLow,
			GateType.NotEqual => !IsEqual( InputA, InputB ) ? GateWireDefinition.GateLogicHigh : GateWireDefinition.GateLogicLow,
			GateType.GreaterThan => numA > numB ? GateWireDefinition.GateLogicHigh : GateWireDefinition.GateLogicLow,
			GateType.LessThan => numA < numB ? GateWireDefinition.GateLogicHigh : GateWireDefinition.GateLogicLow,
			GateType.GreaterEqual => numA >= numB ? GateWireDefinition.GateLogicHigh : GateWireDefinition.GateLogicLow,
			GateType.LessEqual => numA <= numB ? GateWireDefinition.GateLogicHigh : GateWireDefinition.GateLogicLow,

			// Random operations
			GateType.Random => Sandbox.Game.Random.Float( numA, numB ),

			// Mathematical functions
			GateType.Abs => MathF.Abs( numA ),
			GateType.Floor => MathF.Floor( numA ),
			GateType.Ceiling => MathF.Ceiling( numA ),
			GateType.Round => MathF.Round( numA, (int)numB ),
			GateType.Sin => MathF.Sin( numA ),
			GateType.Cos => MathF.Cos( numA ),
			GateType.Tan => MathF.Tan( numA ),
			GateType.Sqrt => numA >= GateWireDefinition.GateLogicLow ? MathF.Sqrt( numA ) : GateWireDefinition.GateLogicLow,
			GateType.Log => numA > GateWireDefinition.GateLogicLow ? MathF.Log( numA ) : GateWireDefinition.GateLogicLow,

			// Utility gates  
			GateType.Clamp => numA < numB ? numB : numA > numC ? numC : numA,
			GateType.Lerp => numA + (numB - numA) * numC,
			GateType.Select => numA > GateWireDefinition.GateBooleanThreshold ? InputB : InputC,
			GateType.Threshold => numA > numB ? GateWireDefinition.GateLogicHigh : GateWireDefinition.GateLogicLow,
			GateType.Invert => -numA,
			GateType.Latch => UpdateSrLatch( boolInputA ),

			// String operations
			GateType.Concat => strA + strB + strC + strD + strE,
			GateType.Length => strA.Length,
			GateType.Substring => ProcessSubstring( strA, numB, numC ),
			GateType.ToUpper => strA.ToUpper(),
			GateType.ToLower => strA.ToLower(),

			// Time functions
			GateType.Time => Time.Now,
			GateType.DeltaTime => Time.Delta,

			// Algebraic logic
			GateType.Vector2 => new Vector2( numA, numB ),
			GateType.Vector3 => new Vector3( numA, numB, numC ),

			// Vector logic
			// More complex gate (Used to extract one element from a Vector depending on the index of this element)
			// Index does not start at 0 for this just to be "user-friendly"
			GateType.VectorGet => InputA switch
			{
				Vector3 v3 => (numB >= 1 && numB <= 3) switch
				{
					true => numB switch
					{
						1 => v3.x,
						2 => v3.y,
						3 => v3.z,
						_ => GateWireDefinition.GateLogicLow
					},
					false => GateWireDefinition.GateLogicLow
				},
				Vector2 v2 => (numB >= 1 && numB <= 2) switch
				{
					true => numB switch
					{
						1 => v2.x,
						2 => v2.y,
						_ => GateWireDefinition.GateLogicLow
					},
					false => GateWireDefinition.GateLogicLow
				},
				_ => GateWireDefinition.GateLogicLow
			},

			_ => GateWireDefinition.GateLogicLow
		};
	}

	private float ConvertToFloat( object? value )
	{
		return value switch
		{
			float f => f,
			int i => i,
			uint ui => ui,
			bool b => b ? 1f : 0f,
			string s when float.TryParse( s, out var f ) => f,
			_ => 0f
		};
	}

	private bool IsEqual( object? a, object? b )
	{
		if ( a is string || b is string )
		{
			return (a?.ToString() ?? "").Equals( b?.ToString() ?? "", StringComparison.Ordinal );
		}

		var numA = ConvertToFloat( a );
		var numB = ConvertToFloat( b );
		return MathF.Abs( numA - numB ) < GateWireDefinition.GateEqualityTolerance;
	}

	private string ProcessSubstring( string input, float startIndex, float length )
	{
		var start = (int)MathF.Max( 0, startIndex );
		var len = (int)MathF.Max( 0, length );

		if ( start >= input.Length )
		{
			return "";
		}

		// Ensure we don't go beyond the string length
		len = Math.Min( len, input.Length - start );

		return input.Substring( start, len );
	}

	private bool UpdateSrLatch( bool inputA )
	{
		if ( inputA )
		{
			_srLatchOutput = !_srLatchOutput;
		}
		return _srLatchOutput;
	}

	/// <summary>
	/// Marks an input as linked - O(1) operation with no iteration.
	/// Called when OnWireInput receives a value.
	/// </summary>
	private void MarkInputAsLinked( string inputId )
	{
		switch ( inputId )
		{
			case "a":
				_inputALinked = true;
				break;
			case "b":
				_inputBLinked = true;
				break;
			case "c":
				_inputCLinked = true;
				break;
			case "d":
				_inputDLinked = true;
				break;
			case "e":
				_inputELinked = true;
				break;
		}
	}

	/// <summary>
	/// Marks an input as unlinked - O(1) operation with no iteration.
	/// Called when OnWireInputDisconnected is triggered.
	/// </summary>
	private void MarkInputAsUnlinked( string inputId )
	{
		switch ( inputId )
		{
			case "a":
				_inputALinked = false;
				break;
			case "b":
				_inputBLinked = false;
				break;
			case "c":
				_inputCLinked = false;
				break;
			case "d":
				_inputDLinked = false;
				break;
			case "e":
				_inputELinked = false;
				break;
		}
	}

	/// <summary>
	/// Initializes the linked state by checking actual wire connections.
	/// Only called once during OnStart to handle gates that were loaded with existing connections.
	/// Uses GetConnections(this) to only check this component's connections.
	/// </summary>
	private void InitializeInputLinkedState()
	{
		// Get only connections involving this component (much more efficient than getting all connections)
		var existingInputs = Wire.Current.GetConnections( this )
			.Where( c => c.Target == this )
			.Select( c => c.InputId )
			.ToHashSet( StringComparer.OrdinalIgnoreCase );

		// Update the state of input (to know if it's linked or not)
		_inputALinked = existingInputs.Contains( "a" );
		_inputBLinked = existingInputs.Contains( "b" );
		_inputCLinked = existingInputs.Contains( "c" );
		_inputDLinked = existingInputs.Contains( "d" );
		_inputELinked = existingInputs.Contains( "e" );
	}
}
