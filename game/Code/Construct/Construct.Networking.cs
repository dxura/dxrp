using System.Text.Json.Nodes;
namespace Dxura.RP.Game;

public partial class Construct
{
	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastSpawn( string stringJson )
	{
		if ( string.IsNullOrWhiteSpace( stringJson ) )
		{
			return;
		}

		if ( JsonNode.Parse( stringJson ) is not JsonObject obj )
		{
			return;
		}

		new GameObject().Deserialize( obj );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastDestroy( GameObject gameObject )
	{
		if ( !gameObject.IsValid() )
		{
			return;
		}

		gameObject.Destroy();
	}
}
