using Dxura.RP.Game.Entities;
using Dxura.RP.Shared;
using System.Threading.Tasks;

namespace Dxura.RP.Game.Commands;

public class SpawnItemCommand : ICommand
{
	public string Command => "spawnitem";
	public string Help => "/spawnitem <item id> [quantity]";
	public bool IsUsableWhileDead => false;
	public Permission[] RequiredPermissions => [Permission.CommandSpawnItem];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( args.Length < 1 )
		{
			caller.SendMessage( Help );
			return true;
		}

		var quantity = 1;
		var itemArgs = args;
		if ( args.Length > 1 && int.TryParse( args[^1], out var parsedQty ) )
		{
			quantity = parsedQty;
			itemArgs = args[..^1];
		}

		if ( quantity <= 0 )
		{
			caller.SendMessage( Help );
			return true;
		}

		if ( !Guid.TryParse( string.Join( ' ', itemArgs ), out var itemId ) )
		{
			caller.Error( "Invalid item id. Use a valid GUID." );
			return true;
		}

		_ = SpawnAsync( caller, itemId, quantity );
		return true;
	}

	private static async Task SpawnAsync( Player caller, Guid itemId, int quantity )
	{
		var definition = await ServerApiClient.GetItemDefinition( itemId );

		await GameTask.MainThread();

		if ( !caller.IsValid() )
		{
			return;
		}

		if ( definition == null )
		{
			caller.Error( "Invalid item id." );
			return;
		}

		var tr = caller.Scene.Trace.Ray( new Ray( caller.AimRay.Position, caller.AimRay.Forward ), 128f )
			.IgnoreGameObjectHierarchy( caller.GameObject.Root )
			.WithoutTags( "trigger" )
			.Run();

		var position = tr.Hit
			? tr.HitPosition + tr.Normal * 12f
			: caller.AimRay.Position + caller.AimRay.Forward * 42f;

		Vector3? velocity = null;
		if ( !tr.Hit )
		{
			velocity = caller.Controller.Velocity + caller.AimRay.Forward * 160f + Vector3.Up * 40f;
		}

		ItemEntity.Create( definition, quantity, position, Rotation.Identity, velocity );
		
		_ = ServerApiClient.Audit( "SpawnItem", $"{caller.SteamName} ({caller.SteamId}) spawned {definition.Name} x{quantity}", caller.SteamId );
	}
}
