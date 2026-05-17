namespace Dxura.RP.Game.Wire;

public record WirePort( string Id, WireType Type )
{
	public static WirePort Input( string id, WireType type )
	{
		return new WirePort( id, type );
	}

	public static WirePort Output( string id, WireType type )
	{
		return new WirePort( id, type );
	}
}
