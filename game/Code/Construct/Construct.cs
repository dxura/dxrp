namespace Dxura.RP.Game;

/// <summary>
/// The system for  constructs in DXRP
/// </summary>
public partial class Construct : GameObjectSystem<Construct>, IGameEvents
{
	internal ConstructDataSerializer Serializer { get; } = new();

	private readonly Dictionary<ConstructType, IConstructDefinition> _definitions = new();

	public Construct( Scene scene ) : base( scene )
	{
		RegisterConstructs();
	}

	private void RegisterConstructs()
	{
		_definitions.Clear();

		// Load all construct definitions
		var definitions = TypeLibrary.GetTypes<IConstructDefinition>();
		foreach ( var definition in definitions.Where( d => !d.IsAbstract && d.TargetType != null ) )
		{
			if ( TypeLibrary.Create<IConstructDefinition>( definition.TargetType ) is {} instance )
			{
				_definitions[instance.Type] = instance;
			}
		}

		Log.Info( $"Registered {definitions.Count} construct defs" );
	}

	public IConstructDefinition? GetDefinition( ConstructType type )
	{
		return _definitions.GetValueOrDefault( type );
	}
}
