namespace Dxura.RP.Game;

public partial class Construct
{
	/// <summary>
	/// Spawn a construct with data. All validation and serialization handled automatically.
	/// </summary>
	public bool SpawnConstructPlayer<T>( ConstructType type, T data, Vector3 positon, Rotation rotation, bool freeze = true ) where T : IConstructData
	{
		var validatedData = ValidateAndSerializePlayer( type, data );
		if ( validatedData == null )
		{
			return false;
		}

		SpawnConstructHost( type, validatedData, positon, rotation, freeze );

		return true;
	}

	/// <summary>
	/// Update the specified construct with new data.
	/// </summary>
	public bool UpdateConstructPlayer<T>( ConstructType type, T data, GameObject target ) where T : IConstructData
	{
		var validatedData = ValidateAndSerializePlayer( type, data );
		if ( validatedData == null )
		{
			return false;
		}

		UpdateConstructHost( target, validatedData );
		return true;
	}

	/// <summary>
	/// Get data from a construct. Returns default data if failed.
	/// </summary>
	public T GetData<T>( IConstruct construct ) where T : IConstructData, new()
	{
		return construct.Data is not T data ? new T() : data;
	}

	/// <summary>
	/// Copy data from source to target construct.
	/// </summary>
	public bool Copy<T>( IConstruct source, IConstruct target ) where T : IConstructData, new()
	{
		var data = GetData<T>( source );
		return UpdateConstruct( target, data );
	}

	private bool UpdateConstruct<T>( IConstruct construct, T data ) where T : IConstructData
	{
		var definition = GetDefinition( construct.Type );
		if ( definition == null )
		{
			return false;
		}

		var validationResult = definition.Validate( data );
		if ( !validationResult.IsValid )
		{
			return false;
		}

		var serializationResult = Serializer.Serialize( construct.Type, data );
		if ( !serializationResult.IsSuccess )
		{
			return false;
		}

		construct.BroadcastData( serializationResult.Value );
		return true;
	}

	private IConstruct? CreateConstruct<T>( long owner, ConstructType type, T data, Vector3 position, Rotation rotation ) where T : IConstructData
	{
		var definition = GetDefinition( type );
		if ( definition == null )
		{
			return null;
		}

		var validationResult = definition.Validate( data );
		if ( !validationResult.IsValid )
		{
			return null;
		}

		return definition.CreateConstruct( owner, data, position, rotation );
	}

	private bool TryGetData<T>( IConstruct construct, out T data ) where T : IConstructData, new()
	{
		if ( construct.Data is T typedData )
		{
			data = typedData;
			return true;
		}

		data = default!;
		return false;
	}

	private string? ValidateAndSerializePlayer<T>( ConstructType type, T data ) where T : IConstructData
	{
		var definition = GetDefinition( type );
		if ( definition == null )
		{
			return null;
		}

		var validationResult = definition.Validate( data );
		if ( !validationResult.IsValid )
		{
			Notify.Error( validationResult.ErrorMessage );
			return null;
		}

		var serializationResult = Serializer.Serialize( type, data );
		return serializationResult.IsSuccess ? serializationResult.Value : null;
	}
}
