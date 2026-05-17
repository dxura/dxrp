using System.Text.Json;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

/// <summary>
/// Centralized (de)serializer for construct data that supports a typed, versioned envelope
/// for forward-compatibility and safe server-side deserialization.
/// </summary>
public sealed class ConstructDataSerializer
{
	private readonly JsonSerializerOptions _options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase, IncludeFields = true, WriteIndented = false
	};

	public SerializationResult<string> Serialize( ConstructType type, IConstructData data )
	{
		try
		{
			var envelope = new ConstructDataEnvelope
			{
				Type = type, Version = data.SchemaVersion, Data = JsonSerializer.SerializeToElement( data, data.GetType(), _options )
			};

			var json = JsonSerializer.Serialize( envelope, _options );
			return SerializationResult<string>.Success( json );
		}
		catch ( Exception ex )
		{
			Log.Error( $"Failed to serialize construct data for type {type}: {ex.Message}" );
			return SerializationResult<string>.Failure( $"Serialization failed: {ex.Message}" );
		}
	}

	public DeserializationResult<IConstructData> Deserialize( string json, Type dataType )
	{
		try
		{
			var envelopeResult = TryReadEnvelope( json );
			if ( !envelopeResult.IsSuccess )
			{
				return DeserializationResult<IConstructData>.Failure( envelopeResult.Error );
			}

			var envelope = envelopeResult.Value;
			return DeserializeElement( envelope.Data, dataType );
		}
		catch ( Exception ex )
		{
			var error = $"Deserialization failed: {ex.Message}";
			Log.Error( error );
			return DeserializationResult<IConstructData>.Failure( error );
		}
	}

	private DeserializationResult<ConstructDataEnvelope> TryReadEnvelope( string json )
	{
		try
		{
			var envelope = JsonSerializer.Deserialize<ConstructDataEnvelope>( json, _options );
			if ( envelope == null )
			{
				return DeserializationResult<ConstructDataEnvelope>.Failure( "Envelope is null" );
			}

			if ( envelope.Data.ValueKind == JsonValueKind.Undefined || envelope.Data.ValueKind == JsonValueKind.Null )
			{
				return DeserializationResult<ConstructDataEnvelope>.Failure( "Envelope data is invalid" );
			}

			return DeserializationResult<ConstructDataEnvelope>.Success( envelope );
		}
		catch ( Exception ex )
		{
			var error = $"Failed to read envelope: {ex.Message}";
			Log.Error( error );
			return DeserializationResult<ConstructDataEnvelope>.Failure( error );
		}
	}

	private DeserializationResult<IConstructData> DeserializeElement( JsonElement element, Type dataType )
	{
		try
		{
			if ( element.ValueKind == JsonValueKind.Undefined || element.ValueKind == JsonValueKind.Null )
			{
				return DeserializationResult<IConstructData>.Failure( "Element is null or undefined" );
			}

			var obj = element.Deserialize( dataType, _options );
			if ( obj is not IConstructData constructData )
			{
				return DeserializationResult<IConstructData>.Failure( $"Deserialized object is not IConstructData: {obj?.GetType()}" );
			}

			return DeserializationResult<IConstructData>.Success( constructData );
		}
		catch ( Exception ex )
		{
			var error = $"Failed to deserialize element to {dataType.Name}: {ex.Message}";
			Log.Error( error );
			return DeserializationResult<IConstructData>.Failure( error );
		}
	}

	public DeserializationResult<IConstructData> DeserializeWithMigration( string json, IConstructDefinition definition )
	{
		var envelopeResult = TryReadEnvelope( json );
		if ( !envelopeResult.IsSuccess )
		{
			return DeserializationResult<IConstructData>.Failure( envelopeResult.Error );
		}

		var envelope = envelopeResult.Value;

		if ( envelope.Type != definition.Type )
		{
			var error = $"Envelope type mismatch. Expected {definition.Type} got {envelope.Type}";
			Log.Error( error );
			return DeserializationResult<IConstructData>.Failure( error );
		}

		if ( envelope.Version == definition.DataSchemaVersion )
		{
			return DeserializeElement( envelope.Data, definition.DataType );
		}

		try
		{
			var migratedData = definition.Migrate( envelope.Version, envelope.Data, this );
			if ( migratedData == null )
			{
				var error = $"Migration from version {envelope.Version} to {definition.DataSchemaVersion} failed for type {definition.Type}";
				Log.Error( error );
				return DeserializationResult<IConstructData>.Failure( error );
			}

			return DeserializationResult<IConstructData>.Success( migratedData );
		}
		catch ( Exception ex )
		{
			var error = $"Migration failed: {ex.Message}";
			Log.Error( error );
			return DeserializationResult<IConstructData>.Failure( error );
		}
	}

	public class ConstructDataEnvelope
	{
		public ConstructType Type { get; set; }
		public uint Version { get; set; }
		public JsonElement Data { get; set; }
	}
}

public readonly struct SerializationResult<T>
{
	public bool IsSuccess { get; }
	public T Value { get; }
	public string Error { get; }

	private SerializationResult( bool isSuccess, T value, string error )
	{
		IsSuccess = isSuccess;
		Value = value;
		Error = error;
	}

	public static SerializationResult<T> Success( T value )
	{
		return new SerializationResult<T>( true, value, string.Empty );
	}
	public static SerializationResult<T> Failure( string error )
	{
		return new SerializationResult<T>( false, default!, error );
	}
}

public readonly struct DeserializationResult<T>
{
	public bool IsSuccess { get; }
	public T Value { get; }
	public string Error { get; }

	private DeserializationResult( bool isSuccess, T value, string error )
	{
		IsSuccess = isSuccess;
		Value = value;
		Error = error;
	}

	public static DeserializationResult<T> Success( T value )
	{
		return new DeserializationResult<T>( true, value, string.Empty );
	}
	public static DeserializationResult<T> Failure( string error )
	{
		return new DeserializationResult<T>( false, default!, error );
	}
}
