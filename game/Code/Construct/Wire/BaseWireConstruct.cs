using Dxura.RP.Game.UI;
namespace Dxura.RP.Game.Wire;

public abstract class BaseWireConstruct( ConstructType type ) : BaseConstruct( type ), IWireComponent, IWireConstruct, IDescription
{
	private readonly List<WirePort> _inputPorts = new();
	private readonly List<WirePort> _outputPorts = new();
	private readonly Dictionary<string, PropertyDescription> _inputHandlers = new();

	public abstract string Name { get; }
	public string DisplayName => Name;

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsPreview )
		{
			return;
		}

		DiscoverPorts();

		if ( !Networking.IsHost )
		{
			return;
		}

		Wire.Current.RegisterComponent( this );
	}

	private void DiscoverPorts()
	{
		var typeDesc = TypeLibrary.GetType( GetType() );

		// Discover input properties
		foreach ( var property in typeDesc.Properties )
		{
			var inputAttr = property.GetCustomAttribute<WireInputAttribute>();
			if ( inputAttr == null )
			{
				continue;
			}

			var wireType = WireType.FromType( property.PropertyType );
			RegisterInputPort( inputAttr.Id, wireType, property );
		}

		// Discover output properties
		foreach ( var property in typeDesc.Properties )
		{
			var outputAttr = property.GetCustomAttribute<WireOutputAttribute>();
			if ( outputAttr == null )
			{
				continue;
			}

			var wireType = WireType.FromType( property.PropertyType );
			RegisterOutputPort( outputAttr.Id, wireType );
		}
	}

	protected void RegisterInputPort( string id, WireType type, PropertyDescription handler )
	{
		if ( _inputPorts.Any( p => p.Id == id ) )
		{
			Log.Warning( $"Input port '{id}' already exists on {GetType().Name}" );
			return;
		}

		_inputPorts.Add( WirePort.Input( id, type ) );
		_inputHandlers[id] = handler;
	}

	protected void RegisterOutputPort( string id, WireType type )
	{
		if ( _outputPorts.Any( p => p.Id == id ) )
		{
			Log.Warning( $"Output port '{id}' already exists on {GetType().Name}" );
			return;
		}

		var port = WirePort.Output( id, type );
		_outputPorts.Add( port );
	}

	public virtual IEnumerable<WirePort> GetInputPorts()
	{
		return _inputPorts;
	}
	public virtual IEnumerable<WirePort> GetOutputPorts()
	{
		return _outputPorts;
	}

	public virtual void OnWireInput( string inputId, WireValue value )
	{
		if ( !_inputHandlers.TryGetValue( inputId, out var handler ) )
		{
			return;
		}

		try
		{
			var convertedValue = value.ConvertTo( WireType.FromType( handler.PropertyType ) );
			handler.SetValue( this, convertedValue.Value );
		}
		catch ( Exception ex )
		{
			// Log error or handle gracefully
			Log.Info( $"Error handling wire input {inputId}: {ex.Message}" );
		}
	}

	public virtual void OnWireInputDisconnected( string inputId )
	{
		// Optional: Handle input disconnection if needed
	}

	// CodeGenerator callback for WireOutput property setters
	internal void OnWireOutputSet<T>( WrappedPropertySet<T> p )
	{
		if ( !GameObject.IsValid() )
		{
			return;
		}

		// Set the property value first
		p.Setter( p.Value );

		// Find the output name from the property
		var propertyName = p.PropertyName;
		var property = TypeLibrary.GetType( GetType() ).Properties.FirstOrDefault( prop => prop.Name == propertyName );
		var outputAttr = property?.GetCustomAttribute<WireOutputAttribute>();

		if ( outputAttr != null )
		{
			// Automatically trigger the output when the property is set
			Wire.Current?.SetOutputValue( this, outputAttr.Id, p.Value );
		}
	}

	public virtual Vector3 GetPortPosition()
	{
		return WorldPosition;
	}

	protected override void OnDestroy()
	{
		if ( IsPreview )
		{
			base.OnDestroy();
			return;
		}

		if ( Networking.IsHost )
		{
			Wire.Current?.UnregisterComponent( this );
		}

		base.OnDestroy();
	}
}
