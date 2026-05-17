using Dxura.RP.Game.Tools;
using Sandbox.Diagnostics;
namespace Dxura.RP.Game.Wire;

// Handle the wire system, managing connections and processing inputs/outputs.
public class Wire : GameObjectSystem<Wire>
{
	public const int WireMaxIterationsPerComponent = 75; // Prevent infinite loops per component
	public const int WireTotalMaxIterations = WireMaxIterationsPerComponent * 64; // Global safety net
	public const uint MaxWireAnchorCount = 10;
	public const float MinWireThickness = 0.4f;
	public const float MaxWireThickness = 1f;
	public const float MinWireOpacity = 0.2f;
	public const float MaxWireOpacity = 1f;

	[ConVar( "dx_wire_render_always", ConVarFlags.Saved )]
	private static bool AlwaysRenderWires { get; set; } = false;

	[ConVar( "dx_wire_render_distance", ConVarFlags.Saved )]
	private static float WireRenderDistance { get; set; } = 1000f;

	private readonly Dictionary<IWireComponent, Dictionary<string, WireValue>> _outputValues = new();
	private readonly Dictionary<IWireComponent, Dictionary<string, WireValue>> _inputValues = new();

	private readonly Queue<(IWireComponent component, string output, WireValue value)> _updateQueue = new();

	public float? WireTickOverride { get; set; }

	[Property]
	[Sync( SyncFlags.FromHost )]
	private NetList<WireConnection> Connections { get; set; } = new();

	// Index for fast output propagation lookup: (component, outputId) -> list of connection IDs
	private readonly Dictionary<(IWireComponent, string), List<Guid>> _outputConnections = new();

	private readonly List<SceneLineObject> _lineObjectPool = new();
	private int _activeLineCount;

	private TimeSince _lastWireTick;

	public Wire( Scene scene ) : base( scene )
	{
		Listen( Stage.FinishUpdate, 20, ProcessWire, "ProcessWire" );
		Listen( Stage.FinishUpdate, 21, RenderWire, "RenderWire" );
	}

	// Main execution method for processing wires.
	private void ProcessWire()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( _lastWireTick < (WireTickOverride ?? Config.Current.Game.WireTick) )
		{
			return;
		}

		_lastWireTick = 0f;

		IWireEvents.Post( x => x.OnWireTick() );

		// Broadcast pre-wire propagate event
		IWireEvents.Post( x => x.OnPreWirePropagate() );

		var componentOutputIterations = new Dictionary<IWireComponent, int>();
		var componentInputIterations = new Dictionary<IWireComponent, int>();

		var totalIterations = 0;
		const int maxInputUpdatesPerTick = 20;

		while ( _updateQueue.Count > 0 && totalIterations++ < WireTotalMaxIterations )
		{
			var (component, outputId, value) = _updateQueue.Dequeue();

			componentOutputIterations.TryGetValue( component, out var outputCount );

			if ( outputCount >= WireMaxIterationsPerComponent )
			{
				Log.Warning( $"Wire component {component} exceeded maximum iterations ({WireMaxIterationsPerComponent}), possible loop detected. Skipping further updates." );

				// Remove all pending updates for this component to prevent queue buildup
				var remainingQueue = new Queue<(IWireComponent component, string output, WireValue value)>();
				while ( _updateQueue.Count > 0 )
				{
					var item = _updateQueue.Dequeue();
					if ( item.component != component )
					{
						remainingQueue.Enqueue( item );
					}
				}

				// Restore queue without the problematic component's updates
				while ( remainingQueue.Count > 0 )
				{
					_updateQueue.Enqueue( remainingQueue.Dequeue() );
				}

				// Notify player
				var owned = component.GameObject.GetComponent<IOwned>();
				if ( owned != null )
				{
					var player = GameUtils.GetPlayerById( owned.Owner );
					if ( player.IsValid() )
					{
						player.Error( $"Recursive loop detected on '{component.Name}'. It has been destroyed..." );
					}
				}

				// Clean up all connections involving this component before destroying it
				UnregisterComponent( component );

				// Get rid of the nasty component
				component.GameObject.Root.Destroy();

				// Purge remaining queue entries that reference the destroyed component
				// to prevent signal propagation into components mid-destruction
				var safeQueue = new Queue<(IWireComponent component, string output, WireValue value)>();
				while ( _updateQueue.Count > 0 )
				{
					var item = _updateQueue.Dequeue();
					if ( item.component != component )
					{
						safeQueue.Enqueue( item );
					}
				}
				while ( safeQueue.Count > 0 )
				{
					_updateQueue.Enqueue( safeQueue.Dequeue() );
				}

				continue;
			}

			componentOutputIterations[component] = outputCount + 1;

			if ( !_outputConnections.TryGetValue( (component, outputId), out var connectionIds ) )
			{
				continue;
			}

			foreach ( var connectionId in connectionIds )
			{
				var connection = Connections.FirstOrDefault( c => c.Id == connectionId );
				if ( connection.Source == null || !connection.Source.GameObject.IsValid() )
				{
					continue;
				}

				if ( connection.Target == null || !connection.Target.GameObject.IsValid() )
				{
					continue;
				}

				componentInputIterations.TryGetValue( connection.Target, out var inputCount );
				if ( inputCount >= maxInputUpdatesPerTick )
				{
					continue;
				}
				componentInputIterations[connection.Target] = inputCount + 1;

				var targetPort = connection.Target.GetInputPorts().FirstOrDefault( p => p.Id == connection.InputId );
				if ( targetPort != null && value.Type.CanConnectTo( targetPort.Type ) )
				{
					var convertedValue = value.ConvertTo( targetPort.Type );
					SetInputValue( connection.Target, connection.InputId, convertedValue );
				}
			}
		}

		if ( totalIterations >= WireTotalMaxIterations )
		{
			Log.Warning( $"Wire processing hit global iteration limit ({WireTotalMaxIterations}). This indicates a serious issue with the wire system." );
		}

		IWireEvents.Post( x => x.OnPostWirePropagate() );
	}

	private void RenderWire()
	{
		if ( (!Player.Local.IsValid() || !WireTool.IsDeployed || WireRenderDistance == 0f) && !AlwaysRenderWires )
		{
			ClearAllWireLines();
			return;
		}

		_activeLineCount = 0;

		foreach ( var connection in Connections )
		{
			if ( connection.Source == null || connection.Target == null || !connection.Source.GameObject.IsValid() || !connection.Target.GameObject.IsValid() )
			{
				continue;
			}

			// Skip connection if it's currently being highlighted by WireTool
			if ( WireTool.HighlightedConnectionIds.Contains( connection.Id ) )
			{
				continue;
			}

			var distanceToCamera = connection.Source.GetPortPosition().DistanceSquared( Player.Local.WorldPosition );

			// Skip connections that are too far away
			if ( distanceToCamera > WireRenderDistance * WireRenderDistance )
			{
				continue;
			}

			// Skip self-connections
			if ( connection.Source == connection.Target )
			{
				continue;
			}

			var fromPosition = connection.Source.GetPortPosition();
			var toPosition = connection.Target.GetPortPosition();

			// Get or create a line object from pool
			SceneLineObject lineObject;
			if ( _activeLineCount < _lineObjectPool.Count )
			{
				lineObject = _lineObjectPool[_activeLineCount];
			}
			else
			{
				lineObject = new SceneLineObject( Scene.SceneWorld )
				{
					EndCap = SceneLineObject.CapStyle.Rounded, StartCap = SceneLineObject.CapStyle.Rounded, Lighting = false, Material = Material.Load( "materials/default/default_line.vmat" )
				};
				_lineObjectPool.Add( lineObject );
			}

			_activeLineCount++;

			RenderWireLine( lineObject, fromPosition, toPosition, connection.Anchors, connection.Color, connection.Thickness, connection.Opacity );
		}

		// Clear unused line objects from pool
		for ( var i = _lineObjectPool.Count - 1; i >= _activeLineCount; i-- )
		{
			_lineObjectPool[i].Delete();
			_lineObjectPool.RemoveAt( i );
		}
	}

	private static void AddWireSegmentToLine( SceneLineObject lineObject, Vector3 fromPosition, Vector3 toPosition, IEnumerable<Vector3>? anchors, Color color, float thickness = 1f, float opacity = 1f )
	{
		// Ensure to get fail answer if thickness has not been set for any reason
		if ( thickness is < MinWireThickness or > MaxWireThickness )
		{
			Log.Warning( $"Invalid thickness {thickness}, clamping to valid range" );
			thickness = Math.Clamp( thickness, MinWireThickness, MaxWireThickness );
		}

		// Ensure to get fail answer if opacity has not been set for any reason
		if ( opacity is < MinWireOpacity or > MaxWireOpacity )
		{
			Log.Warning( $"[Wire.cs] Someone tried to use [{opacity}f] as opacity for wire connection. Opacity has been set to [{MaxWireOpacity}f] !" );
			opacity = Math.Clamp( opacity, MinWireOpacity, MaxWireOpacity );
		}

		// Ensure to set the opacity depending on the player input (or default value if error)
		color = new Color( color.r, color.g, color.b, opacity );

		// Add wire segment points to the existing line
		lineObject.AddLinePoint( fromPosition, color, thickness );

		if ( anchors != null )
		{
			// Add anchors if available
			foreach ( var anchor in anchors )
			{
				lineObject.AddLinePoint( anchor, color, thickness );
			}
		}

		lineObject.AddLinePoint( toPosition, color, thickness );
	}

	private void ClearAllWireLines()
	{
		if ( _lineObjectPool.Count == 0 )
		{
			return;
		}

		foreach ( var lineObject in _lineObjectPool )
		{
			lineObject.Delete();
		}

		_lineObjectPool.Clear();
		_activeLineCount = 0;
	}

	public static void RenderWireLine( SceneLineObject lineObject, Vector3 fromPosition, Vector3 toPosition, IEnumerable<Vector3>? anchors, Color color, float thickness = 1f, float opacity = 1f )
	{
		// Recreate the line with current positions and anchors
		lineObject.StartLine();
		AddWireSegmentToLine( lineObject, fromPosition, toPosition, anchors, color, thickness, opacity );
		lineObject.EndLine();
	}

	public void RegisterComponent( IWireComponent component )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		_outputValues[component] = new Dictionary<string, WireValue>();
		_inputValues[component] = new Dictionary<string, WireValue>();

		// Initialize with default values
		foreach ( var output in component.GetOutputPorts() )
		{
			_outputValues[component][output.Id] = WireValue.Empty( output.Type );
		}
		foreach ( var input in component.GetInputPorts() )
		{
			_inputValues[component][input.Id] = WireValue.Empty( input.Type );
		}
	}

	public void UnregisterComponent( IWireComponent component )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		// Remove all connections involving this component
		var connectionsToRemove = Connections.Where( c => c.Source == component || c.Target == component ).ToList();
		foreach ( var connection in connectionsToRemove )
		{
			Disconnect( connection );
		}

		_outputValues.Remove( component );
		_inputValues.Remove( component );
	}

	[Rpc.Host]
	public void RequestConnect( IWireComponent source, string outputId, IWireComponent target, string inputId, List<Vector3>? anchors, Color color = default, float thickness = 1f, float opacity = 1f )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:wire:connect", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		if ( source == null || target == null || !source.GameObject.IsValid() || !target.GameObject.IsValid() )
		{
			return; // Ensure both are valid
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !GameUtils.HasPermission( player.SteamId, source.GameObject ) || !GameUtils.HasPermission( player.SteamId, target.GameObject, false ) )
		{
			player.Error( "#generic.permission" );
			return;
		}

		if ( anchors?.Count > MaxWireAnchorCount )
		{
			return;
		}

		thickness = Math.Clamp( thickness, MinWireThickness, MaxWireThickness );
		opacity = Math.Clamp( opacity, MinWireOpacity, MaxWireOpacity );

		var connection = Connect( source, outputId, target, inputId, anchors, color, thickness, opacity );

		if ( connection == null )
		{
			player.Error( "#notify.wire.fail" );
		}
		else
		{
			// Add undo action for this connection
			var undoAction = new WireConnectionAction( connection.Value );
			Undo.Current?.AddUndo( player.SteamId, undoAction );

			player.Success( "#notify.wire.connected" );
		}
	}

	public WireConnection? Connect( IWireComponent source, string outputId, IWireComponent target, string inputId, IEnumerable<Vector3>? anchors, Color color = default, float thickness = 1f, float opacity = 1f )
	{
		Assert.True( Networking.IsHost );

		var sourcePort = source.GetOutputPorts().FirstOrDefault( p => p.Id == outputId );
		var targetPort = target.GetInputPorts().FirstOrDefault( p => p.Id == inputId );

		if ( sourcePort == null || targetPort == null )
		{
			return null;
		}

		if ( !sourcePort.Type.CanConnectTo( targetPort.Type ) )
		{
			return null;
		}

		if ( !source.GameObject.IsValid() || !target.GameObject.IsValid() )
		{
			return null; // Ensure both are valid
		}

		// Prevent duplicate connections (same source output to same target input)
		var duplicate = Connections.Any( c => c.Source == source && c.OutputId == outputId && c.Target == target && c.InputId == inputId );
		if ( duplicate )
		{
			return null;
		}

		// Only bool inputs support multiple connections (OR fan-in).
		// For all other types, replace the existing connection.
		if ( targetPort.Type != WireType.Bool )
		{
			var existingConnection = Connections.FirstOrDefault( c => c.Target == target && c.InputId == inputId );
			if ( existingConnection.Source != null )
			{
				Disconnect( existingConnection );
			}
		}

		var connection = new WireConnection( source, outputId, target, inputId )
		{
			Id = Guid.NewGuid(),
			Color = color,
			Thickness = thickness,
			Opacity = opacity,
			Anchors = anchors?.ToArray() ?? []
		};

		// Update output index on host
		var outputKey = (connection.Source, OutputId: connection.OutputId);
		if ( !_outputConnections.TryGetValue( outputKey, out var list ) )
		{
			list = new List<Guid>();
			_outputConnections[outputKey] = list;
		}
		list.Add( connection.Id );

		Connections.Add( connection );

		// Propagate current value
		var currentValue = _outputValues[source][outputId];
		if ( currentValue != null && targetPort != null && currentValue.Value != null )
		{
			var convertedValue = currentValue.ConvertTo( targetPort.Type );
			SetInputValue( target, inputId, convertedValue );
		}

		return connection;
	}

	[Rpc.Host]
	public void RequestDisconnect( IWireComponent source, string outputId, IWireComponent target, string inputId )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:wire:disconnect", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !GameUtils.HasPermission( player.SteamId, source.GameObject ) || !GameUtils.HasPermission( player.SteamId, target.GameObject ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		var connection = Connections.FirstOrDefault( c => c.Source == source && c.OutputId == outputId && c.Target == target && c.InputId == inputId );

		if ( connection.Source == null || connection.Target == null )
		{
			return;
		}

		Disconnect( connection );
		player.Success( "#notify.wire.disconnected" );
	}

	[Rpc.Host]
	public void RequestDisconnectAll( IWireComponent target, string inputId )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:wire:disconnect", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !GameUtils.HasPermission( player.SteamId, target.GameObject ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		var connections = Connections
			.Where( c => c.Target == target && c.InputId == inputId && c.Source != null && c.Target != null )
			.ToList();

		if ( connections.Count == 0 )
		{
			return;
		}

		// Only disconnect wires where the player has permission on both ends
		foreach ( var connection in connections )
		{
			if ( !GameUtils.HasPermission( player.SteamId, connection.Source.GameObject ) )
			{
				continue;
			}

			Disconnect( connection );
		}

		player.Success( "#notify.wire.disconnected" );
	}

	public void Disconnect( WireConnection connection )
	{
		if ( connection.Source == null || connection.Target == null )
		{
			return;
		}

		var target = connection.Target;
		var inputId = connection.InputId;

		// Remove from output index on host
		var outputKey = (connection.Source, OutputId: connection.OutputId);
		if ( _outputConnections.TryGetValue( outputKey, out var list ) )
		{
			list.Remove( connection.Id );
			if ( list.Count == 0 )
			{
				_outputConnections.Remove( outputKey );
			}
		}

		Connections.Remove( connection );

		// Check if there are remaining connections to this input
		var hasRemaining = Connections.Any( c => c.Target == target && c.InputId == inputId );

		if ( hasRemaining )
		{
			// Re-aggregate the remaining connections
			var targetPort = target.GetInputPorts().FirstOrDefault( p => p.Id == inputId );
			if ( targetPort != null && targetPort.Type == WireType.Bool )
			{
				SetInputValue( target, inputId, GetAggregatedBoolInput( target, inputId ) );
			}
		}
		else
		{
			// No remaining connections — reset to default and notify
			var targetPort = target.GetInputPorts().FirstOrDefault( p => p.Id == inputId );
			if ( targetPort != null )
			{
				SetInputValue( target, inputId, WireValue.Empty( targetPort.Type ) );
			}

			target.OnWireInputDisconnected( inputId );
		}
	}

	public void SetOutputValue<T>( IWireComponent component, string outputId, T value )
	{
		if ( !_outputValues.ContainsKey( component ) )
		{
			return;
		}

		var outputPort = component.GetOutputPorts().FirstOrDefault( p => p.Id == outputId );
		if ( outputPort == null )
		{
			return;
		}

		var wireValue = WireValue.Create( value );

		if ( !_outputValues.TryGetValue( component, out var outputDict ) || !outputDict.TryGetValue( outputId, out var oldValue ) )
		{
			return;
		}

		// Prevent unnecessary updates if value hasn't changed
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if ( oldValue.Value != null && oldValue.Value.Equals( wireValue.Value ) )
		{
			return; // No change
		}

		_outputValues[component][outputId] = wireValue;

		// Queue propagation
		_updateQueue.Enqueue( (component, outputId, wireValue) );
	}

	private void SetInputValue( IWireComponent component, string inputId, WireValue value )
	{
		if ( !_inputValues.TryGetValue( component, out _ ) )
		{
			return;
		}

		// For bool inputs with multiple connections, aggregate using OR
		if ( value.Type == WireType.Bool )
		{
			value = GetAggregatedBoolInput( component, inputId );
		}

		_inputValues[component][inputId] = value;

		// Trigger component input handler
		component.OnWireInput( inputId, value );
	}

	private WireValue GetAggregatedBoolInput( IWireComponent target, string inputId )
	{
		foreach ( var connection in Connections )
		{
			if ( connection.Target != target || connection.InputId != inputId )
			{
				continue;
			}

			if ( connection.Source == null || !_outputValues.TryGetValue( connection.Source, out var outputs ) )
			{
				continue;
			}

			if ( outputs.TryGetValue( connection.OutputId, out var val ) && val.Value != null )
			{
				var asBool = val.ConvertTo( WireType.Bool );
				if ( asBool.Value is true )
				{
					return WireValue.Create( true );
				}
			}
		}

		return WireValue.Create( false );
	}

	public WireValue GetOutputValue( IWireComponent component, string outputId )
	{
		return _outputValues.GetValueOrDefault( component )?.GetValueOrDefault( outputId ) ??
		       WireValue.Empty( WireType.Any );
	}

	public WireValue GetInputValue( IWireComponent component, string inputId )
	{
		return _inputValues.GetValueOrDefault( component )?.GetValueOrDefault( inputId ) ??
		       WireValue.Empty( WireType.Any );
	}

	public IEnumerable<WireConnection> GetConnections()
	{
		return Connections;
	}

	public IEnumerable<WireConnection> GetConnections( IWireComponent component )
	{
		return Connections.Where( c => c.Source == component || c.Target == component );
	}

	public IEnumerable<WireConnection> GetSourceConnections( IWireComponent component )
	{
		return Connections.Where( c => c.Source == component );
	}
}
