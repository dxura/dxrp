using Dxura.RP.Shared;
using Sandbox.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dxura.RP.Game.Commands;

public class UseInventoryItemCommand : ICommand
{
	private static readonly SemaphoreSlim UseSemaphore = new( 1, 1 );

	public static string Name => "useitem";
	public string Command => Name;
	public string Help => "/useitem <item id>";
	public bool IsUsableWhileDead => false;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( args.Length != 1 || !Guid.TryParse( args[0], out var itemId ) )
		{
			caller.SendMessage( Help );
			return true;
		}

		_ = UseAsync( caller, itemId );
		return true;
	}

	private static async Task UseAsync( Player caller, Guid itemId )
	{
		Assert.True( Networking.IsHost );
		
		await UseSemaphore.WaitAsync();
		try
		{
			var inventory = await ServerApiClient.GetPlayerInventory( caller.SteamId );
			await GameTask.MainThread();

			if ( !caller.IsValid() )
			{
				return;
			}

			var item = inventory?.FirstOrDefault( x =>
				x.Definition.Id == itemId &&
				x.Quantity > 0 &&
				x.Definition.Type == ItemType.Consumable );

			if ( item == null )
			{
				caller.Error( "You do not have that consumable item." );
				return;
			}

			if ( string.IsNullOrWhiteSpace( item.Definition.GrantIdentifier ) )
			{
				caller.Error( "This consumable item is not configured." );
				return;
			}

			var taken = await ServerApiClient.TakePlayerItem( caller.SteamId, new TakeItemDto
			{
				ItemId = item.Definition.Id,
				Quantity = 1
			} );

			await GameTask.MainThread();
			if ( !caller.IsValid() )
			{
				return;
			}

			if ( !taken )
			{
				caller.Error( "Failed to consume item." );
				return;
			}

			var entityDto = GameModeEntities.FindByIdentifier( item.Definition.GrantIdentifier );

			if ( !entityDto.IsValid() )
			{
				await Refund(caller, itemId );
				return;
			}

			// Check limit (TODO Consolidate):
			var totalEntities = Sandbox.Game.ActiveScene.Components.GetAll<BaseEntity>( FindMode.EverythingInChildren )
				.Count( x => x.Owner == caller.SteamId && x.GameModeEntityId == entityDto!.Id );

			if ( entityDto.Limit > 0 && totalEntities >= entityDto.Limit )
			{
				await Refund(caller, itemId );
				return;
			}

			var prefab = GameObject.GetPrefab( entityDto.PrefabPath() );
			if ( !prefab.IsValid() )
			{
				await Refund(caller, itemId );
				return;
			}

			var entityToSpawn = prefab.Clone();

			entityToSpawn.WorldPosition = GameUtils.GetSpawnPosition( caller.AimRay );

			var baseEntityComponent = entityToSpawn.GetComponent<BaseEntity>();
			if ( baseEntityComponent != null )
			{
				baseEntityComponent.Identifier = entityDto.Identifier();
				baseEntityComponent.Owner = caller.SteamId;
				baseEntityComponent.ConfigureGameModeEntityHost( entityDto );
			}

			entityToSpawn.NetworkSpawn( caller.Connection );
			GameManager.Instance.PurchaseSound?.Broadcast( entityToSpawn.WorldPosition, entityToSpawn );

			var expectedQuantity = Math.Max( 0, item.Quantity - 1 );
			using ( Rpc.FilterInclude( c => c.Id == caller.ConnectionId ) )
			{
				caller.BroadcastInventoryRefresh( item.Definition.Id, expectedQuantity );
			}

			caller.Inventory( string.Format(
				Language.GetPhrase( "notify.inventory.used" ),
				ResolvePhrase( item.Definition.Name ) ) );
			_ = ServerApiClient.Audit( "UseItem", $"{caller.SteamName} ({caller.SteamId}) used {item.Definition.Name} ({item.Definition.Id})", caller.SteamId );
		}
		finally
		{
			UseSemaphore.Release();
		}
	}

	private static async Task Refund( Player caller, Guid itemId )
	{
		await ServerApiClient.GivePlayerItem( caller.SteamId, new GiveItemDto { ItemId = itemId, Quantity = 1 } );
		await GameTask.MainThread();
		
		if ( caller.IsValid() )
		{
			caller.Error( "Failed to spawn entity; item refunded." );
			using ( Rpc.FilterInclude( c => c.Id == caller.ConnectionId ) )
			{
				caller.BroadcastInventoryRefresh( itemId, 1 );
			}
		}
	}

	private static string ResolvePhrase( string text )
	{
		return text.StartsWith( '#' ) ? Language.GetPhrase( text[1..] ) : text;
	}
}
