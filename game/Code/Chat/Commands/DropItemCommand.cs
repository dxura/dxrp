using Dxura.RP.Game.Entities;
using Dxura.RP.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Dxura.RP.Game.Commands;

public class DropItemCommand : ICommand
{
	private static readonly SemaphoreSlim DropSemaphore = new( 1, 1 );

	public static string Name => "dropitem";
	public string Command => Name;
	public string Help => "/dropitem <item id|name> [quantity]";
	public bool IsUsableWhileDead => false;
	public Permission[] RequiredPermissions => [Permission.CommandDropItem];

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
		if ( args.Length > 1 && int.TryParse( args[^1], out var parsedQuantity ) )
		{
			quantity = parsedQuantity;
			itemArgs = args[..^1];
		}

		var query = string.Join( ' ', itemArgs ).Trim();
		if ( quantity <= 0 || string.IsNullOrWhiteSpace( query ) )
		{
			caller.SendMessage( Help );
			return true;
		}

		_ = DropAsync( caller, query, quantity );
		return true;
	}

	private static async Task DropAsync( Player caller, string query, int quantity )
	{
		var inventory = await ServerApiClient.GetPlayerInventory( caller.SteamId );
		await GameTask.MainThread();

		if ( !caller.IsValid() )
		{
			return;
		}

		if ( inventory == null || inventory.Count == 0 )
		{
			caller.Error( "Your inventory is empty." );
			return;
		}
		

		var item = ResolveInventoryItem( inventory, query, caller );
		if ( item == null )
		{
			return;
		}

		var originalQuantity = item.Quantity;

		if ( !item.Definition.IsTradable )
		{
			caller.Error( $"{item.Definition.Name} cannot be dropped." );
			return;
		}

		if ( item.Quantity < quantity )
		{
			caller.Error( $"You only have x{item.Quantity} {item.Definition.Name}." );
			RefreshInventoryUi( caller, item.Definition.Id, originalQuantity );
			return;
		}
		
		// Clear grants
		if ( item.Definition.Type == ItemType.Title &&
		     ( caller.PreferredTitle == item.Definition.GrantIdentifier ||
		       caller.PreferredTitle == item.Definition.Name ) )
		{
			caller.PreferredTitle = null;
		}

		await DropSemaphore.WaitAsync();
		try
		{
			if ( !caller.IsValid() )
			{
				return;
			}

			if ( !ItemEntity.CanCreateDrop( caller.Scene, caller.SteamId ) )
			{
				caller.Error( $"You can only have {ItemEntity.DropLimit} dropped items at once." );
				RefreshInventoryUi( caller, item.Definition.Id, originalQuantity );
				return;
			}

			var expectedRemaining = item.Quantity - quantity;
			var taken = await ServerApiClient.TakePlayerItem( caller.SteamId, new TakeItemDto
			{
				ItemId = item.Definition.Id,
				Quantity = quantity
			} );

			await GameTask.MainThread();

			if ( !caller.IsValid() )
			{
				return;
			}

			if ( !taken )
			{
				caller.Error( "Failed to take item from inventory." );
				RefreshInventoryUi( caller, item.Definition.Id, originalQuantity );
				return;
			}

			var verifiedInventory = await ServerApiClient.GetPlayerInventory( caller.SteamId );
			await GameTask.MainThread();

			if ( !caller.IsValid() )
			{
				return;
			}

			if ( !VerifyItemWasTaken( verifiedInventory, item.Definition.Id, expectedRemaining ) )
			{
				caller.Error( "Failed to verify item was removed. Drop cancelled." );
				RefreshInventoryUi( caller, item.Definition.Id, GetInventoryQuantity( verifiedInventory, item.Definition.Id ) );
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
			
			ItemEntity.Create( item.Definition, quantity, position, Rotation.Identity, velocity, caller );
			_ = ServerApiClient.Audit( "DropItem", $"{caller.SteamName} ({caller.SteamId}) dropped {quantity}x {item.Definition.Name} ({item.Definition.Id})", caller.SteamId );
			RefreshInventoryUi( caller, item.Definition.Id, expectedRemaining );
			caller.Inventory( string.Format(
				Language.GetPhrase( "notify.inventory.dropped" ),
				quantity,
				ResolvePhrase( item.Definition.Name ) ) );
		}
		finally
		{
			DropSemaphore.Release();
		}
	}

	private static string ResolvePhrase( string text )
	{
		return text.StartsWith( '#' ) ? Language.GetPhrase( text[1..] ) : text;
	}

	private static InventoryItemDto? ResolveInventoryItem( IReadOnlyList<InventoryItemDto> inventory, string query, Player caller )
	{
		if ( Guid.TryParse( query, out var itemId ) )
		{
			var item = inventory.FirstOrDefault( x => x.Definition.Id == itemId );
			if ( item == null )
			{
				caller.Error( "Item not found in inventory." );
			}

			return item;
		}

		var exactMatches = inventory
			.Where( x => string.Equals( x.Definition.Name, query, StringComparison.OrdinalIgnoreCase ) )
			.ToList();
		if ( exactMatches.Count == 1 )
		{
			return exactMatches[0];
		}

		if ( exactMatches.Count > 1 )
		{
			caller.Error( "Multiple items matched. Use the item id." );
			return null;
		}

		var partialMatches = inventory
			.Where( x => x.Definition.Name.Contains( query, StringComparison.OrdinalIgnoreCase ) )
			.ToList();
		if ( partialMatches.Count == 1 )
		{
			return partialMatches[0];
		}

		if ( partialMatches.Count > 1 )
		{
			var names = string.Join( ", ", partialMatches.Take( 5 ).Select( x => x.Definition.Name ) );
			caller.Error( $"Multiple items matched: {names}" );
			return null;
		}

		caller.Error( "Item not found in inventory." );
		return null;
	}

	private static bool VerifyItemWasTaken( IReadOnlyList<InventoryItemDto>? inventory, Guid itemId, int expectedRemaining )
	{
		if ( inventory == null )
		{
			return false;
		}

		var remaining = inventory
			.Where( x => x.Definition.Id == itemId )
			.Sum( x => x.Quantity );

		return remaining == expectedRemaining;
	}

	private static int GetInventoryQuantity( IReadOnlyList<InventoryItemDto>? inventory, Guid itemId )
	{
		if ( inventory == null )
		{
			return 0;
		}

		return inventory
			.Where( x => x.Definition.Id == itemId )
			.Sum( x => x.Quantity );
	}

	private static void RefreshInventoryUi( Player caller, Guid itemId, int expectedQuantity )
	{
		if ( !caller.IsValid() )
		{
			return;
		}

		using ( Rpc.FilterInclude( c => c.Id == caller.ConnectionId ) )
		{
			caller.BroadcastInventoryRefresh( itemId, expectedQuantity );
		}
	}
}
