namespace Dxura.RP.Game.Wire;

public readonly record struct WireValue( object Value, WireType Type )
{
	public static WireValue Create<T>( T value )
	{
		return new WireValue( value!, WireType.FromType<T>() );
	}
	public static WireValue Empty( WireType type )
	{
		return new WireValue( type.DefaultValue!, type );
	}

	public WireValue ConvertTo( WireType targetType )
	{
		return Type == targetType ? this : new WireValue( targetType.ConvertFrom( Value ), targetType );
	}
}
