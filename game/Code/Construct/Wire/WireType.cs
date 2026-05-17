namespace Dxura.RP.Game.Wire;

public abstract record WireType( string Name, Type CSharpType, object? DefaultValue )
{
	public static readonly WireType Bool = new PrimitiveType<bool>( "bool", false );
	public static readonly WireType Number = new PrimitiveType<float>( "number", 0 );
	public static readonly WireType String = new PrimitiveType<string>( "string", "" );
	public static readonly WireType Any = new AnyType();

	private static readonly Dictionary<Type, WireType> TypeMap = new()
	{
		[typeof( bool )] = Bool,
		[typeof( float )] = Number,
		[typeof( int )] = Number,
		[typeof( uint )] = Number,
		[typeof( string )] = String
	};

	public static WireType FromType<T>()
	{
		return FromType( typeof( T ) );
	}
	public static WireType FromType( Type type )
	{
		return TypeMap.GetValueOrDefault( type, Any );
	}

	public abstract object ConvertFrom( object value );
	public virtual bool CanConnectTo( WireType other )
	{
		// Any type can connect to anything
		if ( this == Any || other == Any )
		{
			return true;
		}

		// Same types can connect
		if ( this == other )
		{
			return true;
		}

		// Bool and Number can connect to each other
		if ( this == Bool && other == Number || this == Number && other == Bool )
		{
			return true;
		}

		// String only connects to String, Any, or object inputs
		return false;
	}
}

public record PrimitiveType<T> : WireType
{
	private new T DefaultValue { get; }

	public PrimitiveType( string Name, T DefaultValue ) : base( Name, typeof( T ), DefaultValue )
	{
		this.DefaultValue = DefaultValue;
	}

	public override object ConvertFrom( object value )
	{
		// Handle null values - return default value for the type
		if ( value == null )
		{
			return DefaultValue;
		}

		if ( value is T typed )
		{
			return typed;
		}

		if ( typeof( T ) == typeof( bool ) && value is float f )
		{
			return f != 0f;
		}

		if ( typeof( T ) == typeof( float ) && value is bool b )
		{
			return b ? 1f : 0f;
		}

		if ( typeof( T ) == typeof( float ) && value is int i )
		{
			return (float)i;
		}

		if ( typeof( T ) == typeof( float ) && value is uint ui )
		{
			return (float)ui;
		}

		// Handle string-to-bool conversion
		if ( typeof( T ) == typeof( bool ) && value is string str )
		{
			// Try to parse as a number first, then convert to bool
			if ( float.TryParse( str, out var numValue ) )
			{
				return numValue != 0f;
			}

			// Try to parse as boolean string
			if ( bool.TryParse( str, out var boolValue ) )
			{
				return boolValue;
			}

			// Default to false for unparseable strings
			return false;
		}

		var converted = Convert.ChangeType( value, typeof( T ) );
		return converted;
	}
}

public record AnyType() : WireType( "any", typeof( object ), null )
{
	public override object ConvertFrom( object value )
	{
		return value;
	}
	public override bool CanConnectTo( WireType other )
	{
		return true;
	}
}
