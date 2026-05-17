using Dxura.RP.Game.Equipments;
using Dxura.RP.Shared;
using Sandbox.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dxura.RP.Game.Entities;

[Title( "Inventory Item" )]
[Category( "Entities" )]
public class ItemEntity : BaseEntity, Component.IPressable
{
	public const int DropLimit = 10;
	private static readonly SemaphoreSlim PickupSemaphore = new( 1, 1 );

	[Property]
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnItemDefinitionIdChanged ) )]
	public Guid ItemDefinitionId { get; set; }

	[Property]
	[Sync( SyncFlags.FromHost )]
	public int Quantity { get; set; } = 1;

	[Property]
	public SoundEvent? UseSound { get; set; }

	public ItemDefinitionDto? Definition;

	protected override void OnStart()
	{
		base.OnStart();

		if ( ItemDefinitionId != Guid.Empty )
		{
			_ = LoadDefinition();
		}
	}

	public InventoryItemDto GetInventoryItem()
	{
		return new InventoryItemDto
		{
			Id = ItemDefinitionId,
			Definition = Definition ?? new ItemDefinitionDto
			{
				Id = ItemDefinitionId,
				Name = "Unknown Item",
				ImageUrl = string.Empty,
				Rarity = ItemRarity.Common,
				Type = ItemType.Other,
				IsTradable = true,
				Description = string.Empty
			},
			Quantity = Quantity
		};
	}

	public bool CanPress( IPressable.Event e )
	{
		return true;
	}

	public bool Press( IPressable.Event e )
	{
		// Prevent using while rotating in hands
		var hands = Player.Local.GetComponentInChildren<HandsEquipment>();
		if ( hands.IsValid() && hands.IsHolding( GameObject, true ) )
		{
			return false;
		}
		
		if ( Cooldown.Current.CheckAndStartCooldown( "action", Config.Current.Game.ActionCooldown, true ) )
		{
			return false;
		}

		OnUseHost();
		return true;
	}

	[Rpc.Host]
	private void OnUseHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:action", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		var tr = Scene.Trace.Ray( player.AimRay, Config.Current.Game.ReachDistance )
			.IgnoreGameObjectHierarchy( player.GameObject )
			.UseHitboxes()
			.Run();

		if ( !tr.Hit || tr.GameObject.Root != GameObject.Root )
		{
			return;
		}

		_ = DoPickupHost( player );
	}

	private async Task DoPickupHost( Player player )
	{
		await PickupSemaphore.WaitAsync();

		try
		{
			if ( GameObject.IsDestroyed )
			{
				return;
			}

			var quantity = Math.Max( 1, Quantity );

			if ( Definition is { IsStackable: false } || Definition?.MaxStack.HasValue == true )
			{
				var inventory = await ServerApiClient.GetPlayerInventory( player.SteamId );

				if ( Definition is { IsStackable: false } )
				{
					var alreadyOwns = inventory?.Any( x => x.Definition.Id == ItemDefinitionId ) ?? false;
					if ( alreadyOwns )
					{
						player.Error( string.Format(
							Language.GetPhrase( "notify.inventory.already_owned" ),
							ResolvePhrase( Definition.Name ) ) );
						return;
					}
				}
				else if ( Definition?.MaxStack.HasValue == true )
				{
					var currentQuantity = inventory?
						.Where( x => x.Definition.Id == ItemDefinitionId )
						.Sum( x => x.Quantity ) ?? 0;

					if ( currentQuantity + quantity > Definition.MaxStack.Value )
					{
						var allowable = Definition.MaxStack.Value - currentQuantity;
						if ( allowable <= 0 )
						{
							player.Error( string.Format(
								Language.GetPhrase( "notify.inventory.max_stack" ),
								Definition.MaxStack.Value,
								ResolvePhrase( Definition.Name ) ) );
							return;
						}

						quantity = allowable;
					}
				}
			}

			var item = await ServerApiClient.GivePlayerItem( player.SteamId, new GiveItemDto
			{
				ItemId = ItemDefinitionId,
				Quantity = quantity
			} );

			if ( item == null )
			{
				player.Error( "#notify.inventory.pickup_failed" );
				return;
			}

			player.Inventory( string.Format(
				Language.GetPhrase( "notify.inventory.received" ),
				quantity,
				ResolvePhrase( item.Definition.Name ) ) );
			player.BroadcastInventoryRefresh( item.Definition.Id, item.Quantity );
			_ = ServerApiClient.Audit( "PickupItem", $"{player.SteamName} ({player.SteamId}) picked up {quantity}x {item.Definition.Name} ({item.Definition.Id})", player.SteamId );
			UseSound?.Broadcast( WorldPosition );
			GameObject.Destroy();
		}
		finally
		{
			PickupSemaphore.Release();
		}
	}

	private async Task LoadDefinition()
	{
		Definition = Networking.IsHost
			? await ServerApiClient.GetItemDefinition( ItemDefinitionId )
			: await PlayerApiClient.GetItemDefinition( ItemDefinitionId );
	}

	private void OnItemDefinitionIdChanged( Guid oldValue, Guid newValue )
	{
		if ( newValue == Guid.Empty || oldValue == newValue )
		{
			return;
		}

		_ = LoadDefinition();
	}

	private static string ResolvePhrase( string text )
	{
		return text.StartsWith( '#' ) ? Language.GetPhrase( text[1..] ) : text;
	}
	
	public static int GetOwnedDropCount( Scene scene, long owner )
	{
		return scene.Components.GetAll<ItemEntity>( FindMode.EverythingInChildren )
			.Count( x => x.IsValid() && x.Owner == owner );
	}

	public static bool CanCreateDrop( Scene scene, long owner )
	{
		return GetOwnedDropCount( scene, owner ) < DropLimit;
	}

	public static void Create( ItemDefinitionDto item, int quantity, Vector3 position, Rotation? rotation = null, Vector3? velocity = null, Player? owner = null )
	{
		Assert.True( Networking.IsHost );

		var go = GameManager.Instance.ItemPrefab.Clone();
		go.Name = item.Name;
		go.WorldPosition = position;
		go.WorldRotation = rotation ?? Rotation.Random;

		var entity = go.GetComponent<ItemEntity>();
		entity.ItemDefinitionId = item.Id;
		entity.Quantity = Math.Max( 1, quantity );
		entity.Owner = owner?.SteamId ?? 0;
		entity.Definition = item;

		if ( velocity.HasValue )
		{
			var rigidbody = go.GetComponent<Rigidbody>();
			rigidbody.Velocity = velocity.Value;
			rigidbody.AngularVelocity = Vector3.Random * 6f;
		}

		go.DestroyAsync( Config.Current.Game.DroppedInventoryItemDestroyTime, true );
		go.NetworkSpawn( owner?.Connection );
	}
}
