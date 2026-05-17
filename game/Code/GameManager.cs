using Dxura.RP.Game.Entities;
using Dxura.RP.Game.System.Events;
using Dxura.RP.Game.UI;
using Dxura.RP.Shared;
using Sandbox.Diagnostics;
using Sandbox.Services;
using Sandbox.Utility;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public class GameManager : SingletonComponent<GameManager>, IGameEvents, IConfigEvents
{
	[Property] public required GameObject HudGameObject { get; set; }

	public TimeSince SessionTime { get; private set; } = 0f;
	private TimeSince LastSecondlyUpdate { get; set; } = 0;

	// Events (Modifiers)
	[Property] [Group( "Event Modifiers" )]
	[Sync( SyncFlags.FromHost )]
	public float SalaryMultiplier { get; set; } = 1f;

	[Property] [Group( "Event Modifiers" )]
	[Sync( SyncFlags.FromHost )]
	public float EntityPriceMultiplier { get; set; } = 1f;

	[Property] [Group( "Event Modifiers" )]
	[Sync( SyncFlags.FromHost )]
	public bool IgnoreJobRequirements { get; set; } = false;

	[Property] public required Dictionary<string, GameObject> GameObjects { get; set; } = new();
	[Property] [Group( "Spawns" )] public required GameObject MoneyPrefab { get; set; }
	[Property] [Group( "Spawns" )] public required GameObject PanicPrefab { get; set; }
	[Property] [Group( "Spawns" )] public required GameObject ItemPrefab { get; set; }

	[Property] [Group( "Decals" )] public required List<DecalDefinition> BloodDecals { get; set; }

	[Property] [Group( "Effects" )] public SoundEvent? PurchaseSound { get; set; }
	[Property] [Group( "Effects" )] public SoundEvent? AmmoSound { get; set; }
	[Property] [Group( "Effects" )] public SoundEvent? BreachSound { get; set; }
	[Property] [Group( "Effects" )] public Material? FadedMaterial { get; set; }

	/// <summary>
	/// Headless mode for dedicated servers (wrapped so we can test in editor without needing to launch dedi)
	/// </summary>
	public static bool IsHeadless => Application.IsHeadless;

	protected override void OnStart()
	{
		DxSound.InitializeMixers();

		if ( GameObjects.TryGetValue( Config.Current.Game.Identifier, out var go ) )
		{
			go.Enabled = true;
		}

		Achievements.Unlock( "alpha" );
		Achievements.Unlock( "play_game" );
	}

	protected override void OnUpdate()
	{
		if ( LastSecondlyUpdate < 1 )
		{
			return;
		}

		LastSecondlyUpdate = 0;
		IGameEvents.Post( x => x.OnSecondlyUpdate() );
	}

	public void OnConfigOverride()
	{
		// Load any cloud packages
		foreach ( var package in Config.Current.Game.CloudPackages )
		{
			Cloud.Load( package );
		}
	}

	public void OnGameModeUpdated( GameModeDto? before, GameModeDto? after )
	{
		if ( !Networking.IsHost || after == null )
		{
			return;
		}

		foreach ( var player in GameUtils.Players.ToList() )
		{
			if ( !player.IsValid() )
			{
				continue;
			}

			if ( GameModeJobs.FindById( player.Job?.Id ) != null )
			{
				continue;
			}

			var resolvedJob = !string.IsNullOrWhiteSpace( player.Job?.Name )
				? GameModeJobs.FindByName( player.Job.Name )
				: null;

			player.Job = resolvedJob ?? GameModeJobs.Default;
		}
	}


	public void OnPlayerKillHost( Player player )
	{
		var moneyToDrop = player.WalletBalance;
		var damageInfo = player.LastDamageInfo;
		var attacker = damageInfo?.Attacker.IsValid() == true
			? GameUtils.GetPlayerFromComponent( damageInfo.Attacker )
			: null;

		var description = BuildDeathAuditDescription( player, attacker, damageInfo );

		_ = ServerApiClient.Audit( "Death", description, player.SteamId );

		if ( moneyToDrop == 0 || player.Restricted )
		{
			return;
		}

		player.ClearWalletHost();
		DropMoneyHost( moneyToDrop, player.WorldPosition + Vector3.Up * 30f, $"Player killed: {player.SteamName} ({player.SteamId})" );
	}

	private static string BuildDeathAuditDescription( Player victim, Player? attacker, DamageInfo? damageInfo )
	{
		var victimText = $"{victim.SteamName} ({victim.SteamId})";

		if ( attacker.IsValid() )
		{
			if ( attacker == victim )
			{
				return $"{victimText} died by suicide";
			}

			return $"{victimText} was killed by {attacker.SteamName} ({attacker.SteamId})";
		}

		if ( damageInfo?.WasFallDamage == true )
		{
			return $"{victimText} died from fall damage";
		}

		var inflictorName = damageInfo?.Inflictor.IsValid() == true
			? damageInfo.Inflictor.GameObject.Name
			: null;

		return !string.IsNullOrWhiteSpace( inflictorName )
			? $"{victimText} died due to {inflictorName}"
			: $"{victimText} died";
	}

	public static T? ShowUi<T>() where T : Component, new()
	{
		if ( !Instance.IsValid() )
		{
			return null;
		}

		if ( !Instance.HudGameObject.IsValid() )
		{
			return null;
		}

		return Instance.HudGameObject.GetOrAddComponent<T>();
	}

	public bool RequestOwnership( GameObject gameObject )
	{
		if ( !gameObject.IsValid() )
		{
			return false;
		}

		// Double-check permission on client before attempting to take/use ownership
		if ( !GameUtils.HasPermission( Player.Local.SteamId, gameObject ) )
		{
			return false;
		}

		// No need to take ownership, we are already owner.
		// Check if the local connection is the owner, or if this is a construct and the local player is the construct owner
		if ( gameObject.Network.Owner == Connection.Local ||
		     gameObject.Tags.Has( Constants.ConstructTag ) &&
		     gameObject.GetComponent<BaseConstruct>()?.NetworkOwner == Connection.Local.Id )
		{
			return true;
		}

		// Only allow ownership of Constructs or Entities
		if ( !gameObject.Tags.Has( Constants.ConstructTag ) && !gameObject.Tags.Has( Constants.EntityTag ) )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "ownership:take", Config.Current.Game.ActionQuickCooldown ) )
		{
			return false;
		}

		RequestOwnershipHost( gameObject );

		return true;
	}

	[Rpc.Host]
	private void RequestOwnershipHost( GameObject gameObject )
	{
		var caller = Rpc.Caller;
		var callerId = Rpc.CallerId;

		if ( !gameObject.IsValid() || Cooldown.Current.CheckAndStartCooldown( $"{callerId}:ownership:take", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		// Server-side enforcement in case permissions changed (e.g. unfriended)
		if ( !GameUtils.HasPermission( caller, gameObject ) )
		{
			// Drop ownership if we are the current owner but lost permission
			if ( gameObject.Network.Owner == caller )
			{
				if ( gameObject.NetworkMode == NetworkMode.Object )
				{
					gameObject.Network.DropOwnership();
				}
				else
				{
					var construct = gameObject.GetComponent<BaseConstruct>();
					if ( construct.IsValid() )
					{
						construct.BroadcastSetNetworkOwner( Guid.Empty );
					}
				}
			}

			return; // No permission
		}

		// Only allow ownership of Constructs or Entities
		if ( !gameObject.Tags.Has( Constants.ConstructTag ) && !gameObject.Tags.Has( Constants.EntityTag ) )
		{
			return;
		}

		Log.Info( "Assigning ownership of " + gameObject.Name + " to " + caller.SteamId );

		// Assign ownership based on type
		if ( gameObject.NetworkMode == NetworkMode.Object )
		{
			gameObject.Network.AssignOwnership( caller );
		}
		else
		{
			var construct = gameObject.GetComponent<BaseConstruct>();
			if ( construct.IsValid() )
			{
				construct.BroadcastSetNetworkOwner( callerId );
			}
		}
	}

	[Rpc.Host]
	public void PurchaseEntityHost( GameModeEntityDto entity )
	{
		var callerId = Rpc.CallerId;
		var callerSteamId = Rpc.Caller.SteamId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:entity", Config.Current.Game.EntityCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			return;
		}

		var entityPrefabPath = entity.PrefabPath();
		if ( string.IsNullOrWhiteSpace( entityPrefabPath ) )
		{
            return;
		}
		var marketItem = GameModeMarketItems.All
			.FirstOrDefault( x => x.Type == GameModeMarketItemType.Entity && x.ReferenceId == entity.Id );
		var basePrice = (float)Math.Max( 0, (int)MathF.Ceiling( (marketItem?.Cost ?? 0) * EntityPriceMultiplier ) );
		uint taxAmount = 0;
		if ( Config.Current.Game.GovernanceTaxEnabled && Governance.Current.TaxRate > 0f && !Governance.Current.IsExemptFromTax( player ) )
		{
			return;
		}

		var prefab = GameObject.GetPrefab( entityPrefabPath );
		if ( !prefab.IsValid() )
		{
			return;
		}

		if ( player.Restricted )
		{
			return;
		}

		if ( entity.Limit > 0 && GameModeMarketItems.GetOwnedEntityCount( player, entity.Id ) >= entity.Limit )
		{
			player.Error( "#generic.forbidden" );
			return;
		}

		Log.Info( $"Player {callerSteamId} purchased entity '{entity.DisplayName()}'" );

		var entityToSpawn = prefab.Clone();

		entityToSpawn.WorldPosition = GameUtils.GetSpawnPosition( player.AimRay );

		var baseEntityComponent = entityToSpawn.GetComponent<BaseEntity>();
		if ( baseEntityComponent != null )
		{
			baseEntityComponent.Identifier = entity.Identifier();
			baseEntityComponent.Owner = player.SteamId;
			baseEntityComponent.ConfigureGameModeEntityHost( entity );
		}

		entityToSpawn.NetworkSpawn( player.Connection );
		PurchaseSound?.Broadcast( entityToSpawn.WorldPosition, entityToSpawn );
	}

	[Rpc.Host]
	public async void PurchaseMarketItemHost( Guid marketItemId )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:entity", Config.Current.Game.EntityCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		var marketItem = GameModeMarketItems.FindById( marketItemId );

		if ( !player.IsValid() || marketItem == null )
		{
			return;
		}

		if ( !GameModeMarketItems.CanPurchase( player, marketItem ) )
		{
			player.Error( "#generic.forbidden" );
			return;
		}

		var displayName = GameModeMarketItems.DisplayName( marketItem );
		if ( string.IsNullOrWhiteSpace( displayName ) )
		{
			return;
		}

		var price = (uint)Math.Max( 0, (int)MathF.Ceiling( marketItem.Cost * EntityPriceMultiplier ) );
		if ( price > 0 && !await player.ChargeHost( price, $"Purchased {displayName}", true ) )
		{
			return;
		}

		Log.Info( $"Player {player.SteamId} purchased market item '{displayName}' [{marketItem.Id}] for ${price}" );

		switch ( marketItem.Type )
		{
			case GameModeMarketItemType.Entity:
				var entity = GameModeMarketItems.ResolveEntity( marketItem );
				if ( entity == null )
				{
					return;
				}

				var entityPrefabPath = entity.PrefabPath();
				if ( string.IsNullOrWhiteSpace( entityPrefabPath ) )
				{
					return;
				}

				var entityPrefab = GameObject.GetPrefab( entityPrefabPath );
				if ( !entityPrefab.IsValid() )
				{
					return;
				}

				var entityToSpawn = entityPrefab.Clone();
				entityToSpawn.WorldPosition = GameUtils.GetSpawnPosition( player.AimRay );

				var baseEntityComponent = entityToSpawn.GetComponent<BaseEntity>();
				if ( baseEntityComponent != null )
				{
					baseEntityComponent.Identifier = entity.Identifier();
					baseEntityComponent.Owner = player.SteamId;
					baseEntityComponent.ConfigureGameModeEntityHost( entity );
				}

				entityToSpawn.NetworkSpawn( player.Connection );
				PurchaseSound?.Broadcast( entityToSpawn.WorldPosition, entityToSpawn );
				return;

			case GameModeMarketItemType.Equipment:
				var equipment = GameModeMarketItems.ResolveEquipment( marketItem );
				if ( equipment == null )
				{
					return;
				}

				if ( marketItem.Quantity > 1 )
				{
					var shipmentPrefab = GameObject.GetPrefab( GameModeMarketItems.ShipmentPrefabPath );
					if ( !shipmentPrefab.IsValid() )
					{
						return;
					}

					var shipmentObject = shipmentPrefab.Clone();
					shipmentObject.WorldPosition = GameUtils.GetSpawnPosition( player.AimRay );

					var shipmentEntity = shipmentObject.GetComponent<ShipmentEntity>();
					var shipmentBaseEntity = shipmentObject.GetComponent<BaseEntity>();
					if ( !shipmentEntity.IsValid() || !shipmentBaseEntity.IsValid() )
					{
						shipmentObject.Destroy();
						return;
					}

					shipmentBaseEntity.Owner = player.SteamId;
					shipmentBaseEntity.Identifier = equipment.Identifier();
					shipmentEntity.MarketItemId = marketItem.Id;
					shipmentEntity.ConfigureHost( equipment, marketItem.Quantity );

					shipmentObject.NetworkSpawn( player.Connection );
					PurchaseSound?.Broadcast( shipmentObject.WorldPosition, shipmentObject );
					return;
				}

				var droppedEquipment = DroppedEquipment.CreateHost(
					equipment,
					GameUtils.GetSpawnPosition( player.AimRay ),
					rotation: Rotation.FromYaw( player.Controller.EyeAngles.yaw + 90 ),
					marketItemId: marketItem.Id );
				PurchaseSound?.Broadcast( droppedEquipment.WorldPosition, droppedEquipment.GameObject );
				return;
		}
	}

	[Rpc.Host]
	public void HealHost( bool selfHeal )
	{
		var callerId = Rpc.CallerId;
		var callerSteamId = Rpc.Caller.SteamId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:heal", Config.Current.Game.HealCooldown ) )
		{
			return;
		}

		var healer = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !healer.IsValid() )
		{
			return;
		}

		if ( !healer.Job.IsMedicRole() )
		{
			return;
		}

		var healObj = selfHeal ? healer.GameObject : null;

		if ( !selfHeal )
		{
			var trace = Scene.Trace.Ray( healer.AimRay, Config.Current.Game.ReachDistance )
				.IgnoreGameObjectHierarchy( healer.GameObject )
				.UseHitboxes()
				.Run();

			if ( !trace.Hit || !trace.GameObject.IsValid() )
			{
				return;
			}

			healObj = trace.GameObject.Root;
		}

		if ( !healObj.IsValid() )
		{
			return;
		}

		var targetPlayer = healObj.GetComponent<Player>();
		Log.Info( targetPlayer.IsValid() ?
			(FormattableString)$"Player {callerSteamId} ({healer.GameObject}) healed {targetPlayer.SteamId} ({targetPlayer.DisplayName})"
			:
			(FormattableString)$"Player {callerSteamId} ({healer.GameObject}) healed {healObj.Name}"
		);

		// Config: only allow healing players
		if ( Config.Current.Game.MedkitHealOnlyAllowHealingPlayers && !targetPlayer.IsValid() )
		{
			return;
		}

		var healthComp = healObj.GetComponent<HealthComponent>();

		if ( !healthComp.IsValid() || healthComp.Health >= healthComp.MaxHealth * Config.Current.Game.MedkitOverHealPercent || healthComp.Health <= 0 )
		{
			return;
		}

		var healAmount = Config.Current.Game.MedkitHealAmount * (selfHeal ? Config.Current.Game.MedKitSelfHealReductionPercent : 1f);

		healthComp.Health += healAmount;
		healer.IncrementStat( "healed", (int)Math.Ceiling( healAmount ) );

		if ( healthComp.Health > healthComp.MaxHealth * Config.Current.Game.MedkitOverHealPercent )
		{
			healthComp.Health = healthComp.MaxHealth * Config.Current.Game.MedkitOverHealPercent;
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastPanic( Vector3 position )
	{
		PanicPrefab.Clone( position );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastTagHost( GameObject? gameObject, bool isAdd, params string[] tags )
	{
		if ( !gameObject.IsValid() )
		{
			return;
		}

		if ( isAdd )
		{
			gameObject.Tags.Add( tags );
		}
		else
		{
			foreach ( var tag in tags )
			{
				gameObject.Tags.Remove( tag );
			}
		}
	}

	[Rpc.Host]
	public void DestroyHost( GameObject gameObject )
	{
		var caller = Rpc.Caller;
		var callerId = Rpc.CallerId;
		var ownerBypass = RankSystem.HasPermission( caller.SteamId, Permission.RemoverBypass );

		// Server-side cooldown check - perm bypass
		if ( !ownerBypass && Cooldown.Current.CheckAndStartCooldown( $"{callerId}:destroy", Config.Current.Game.RemoverCooldown ) )
		{
			return;
		}

		if ( !gameObject.IsValid() )
		{
			return;
		}

		// Permanent objects (owner 0) can only be removed by those with Permanent permission
		var ownedObj = gameObject.GetComponent<IOwned>();
		if ( ownedObj != null && ownedObj.Owner == 0 && !RankSystem.HasPermission( caller.SteamId, Permission.Permanent ) )
		{
			return;
		}

		// Entity restriction: players can only destroy restricted entities
		if ( gameObject.Tags.Has( Constants.EntityTag ) && !ownerBypass && !gameObject.Tags.Has( Constants.RestrictedEntity ) )
		{
			return;
		}

		// Permission check: staff bypass, others need permission
		if ( !ownerBypass && !GameUtils.HasPermission( caller, gameObject ) )
		{
			return;
		}

		Log.Info( $"Player {caller.SteamId} destroyed {gameObject.Name} (Bypass: {ownerBypass})" );

		if ( gameObject.NetworkMode == NetworkMode.Object )
		{
			gameObject.Destroy();
			return;
		}

		if ( gameObject.Tags.Has( Constants.ConstructTag ) )
		{
			var construct = gameObject.GetComponent<IConstruct>();
			if ( construct.IsValid() )
			{
				construct.Destroy();
			}
		}
	}

	[Rpc.Host]
	public void SetPermanentHost( Vector3 center, float radius )
	{
		var caller = Rpc.Caller;
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.Permanent ) )
		{
			return;
		}

		var nearbyObjects = Scene.FindInPhysics( new Sphere( center, radius ) );
		if ( nearbyObjects == null )
		{
			return;
		}

		var count = 0;
		var processed = new HashSet<GameObject>();
		foreach ( var gameObject in nearbyObjects )
		{
			var root = gameObject.Root;
			if ( !root.IsValid() || !processed.Add( root ) )
			{
				continue;
			}

			if ( root.Tags.Has( Constants.ConstructTag ) )
			{
				var construct = root.GetComponent<BaseConstruct>();
				if ( construct.IsValid() && construct.Owner != 0 )
				{
					Undo.Current?.RemoveUndoById( construct.Owner, construct.Id );
					Construct.Current?.DecrementCount( construct.Owner, construct.Type );
					construct.BroadcastSetOwner( 0 );
					construct.BroadcastSetNetworkOwner( Guid.Empty );
					count++;
				}
			}
			else if ( root.Tags.Has( Constants.EntityTag ) )
			{
				var entity = root.GetComponent<BaseEntity>();
				if ( entity.IsValid() && entity.Owner != 0 )
				{
					entity.Owner = 0;
					root.Network.DropOwnership();

					// Remove health so entity cannot be destroyed
					var health = root.GetComponent<HealthComponent>();
					if ( health.IsValid() )
					{
						health.Destroy();
					}

					count++;
				}
			}
		}

		Log.Info( $"Player {caller.SteamId} set {count} objects as permanent (center={center}, radius={radius})" );
	}

	[Rpc.Host]
	public void ClearPermanentHost( Vector3 center, float radius )
	{
		var caller = Rpc.Caller;
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.Permanent ) )
		{
			return;
		}

		var nearbyObjects = Scene.FindInPhysics( new Sphere( center, radius ) );
		if ( nearbyObjects == null )
		{
			return;
		}

		var count = 0;
		var processed = new HashSet<GameObject>();
		foreach ( var gameObject in nearbyObjects )
		{
			var root = gameObject.Root;
			if ( !root.IsValid() || !processed.Add( root ) )
			{
				continue;
			}

			if ( root.Tags.Has( Constants.ConstructTag ) )
			{
				var construct = root.GetComponent<BaseConstruct>();
				if ( construct.IsValid() && construct.Owner == 0 )
				{
					construct.Destroy();
					count++;
				}
			}
			else if ( root.Tags.Has( Constants.EntityTag ) )
			{
				var entity = root.GetComponent<BaseEntity>();
				if ( entity.IsValid() && entity.Owner == 0 )
				{
					root.Destroy();
					count++;
				}
			}
		}

		Log.Info( $"Player {caller.SteamId} cleared {count} permanent objects (center={center}, radius={radius})" );
	}

	[Rpc.Host]
	public void ClearAllPermanentHost()
	{
		var caller = Rpc.Caller;
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.Permanent ) )
		{
			return;
		}

		var count = 0;

		foreach ( var construct in Scene.GetAll<IConstruct>().ToList() )
		{
			if ( !construct.IsValid() || construct.Owner != 0 )
			{
				continue;
			}

			construct.Destroy();
			count++;
		}

		foreach ( var entity in Scene.GetAll<BaseEntity>().ToList() )
		{
			if ( !entity.IsValid() || entity.Owner != 0 )
			{
				continue;
			}

			entity.GameObject.Destroy();
			count++;
		}

		Log.Info( $"Player {caller.SteamId} cleared {count} permanent objects" );
	}

	public void DropMoneyHost( uint amount, Vector3 position, string reason, Rotation rotation = default )
	{
		Assert.True( Networking.IsHost );

		_ = ServerApiClient.Audit( "MoneySpawn", $"${amount} spawned: {reason}" );

		var entityToSpawn = MoneyPrefab.Clone();
		entityToSpawn.WorldPosition = position;
		entityToSpawn.WorldRotation = rotation;

		var money = entityToSpawn.GetComponent<MoneyEntity>();
		money.Value = amount;

		// Destroy money after a delay
		entityToSpawn.DestroyAsync( Config.Current.Game.DroppedMoneyDestroyTime );

		entityToSpawn.NetworkSpawn();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastAnnouncementHost( string message, Announcement.AnnouncementType type = Announcement.AnnouncementType.Generic, float? duration = null )
	{
		Announcement.Announce( message, type, duration ?? 10f );
	}

	public void OnPlayerJobChangedHost( Player player, GameModeJobDto before, GameModeJobDto after )
	{
		if ( !Config.Current.Game.JobEntitiesClearOnChange || !Networking.IsHost )
		{
			return;
		}

		// Skip cleanup on initial job assignment (e.g. during player creation before recovery data is loaded)
		if ( before == null )
		{
			return;
		}

		// Don't clear if restricted
		if ( player.Restricted )
		{
			return;
		}

		CleanupSystem.Current.CleanupJobEntities( player.SteamId, after );
	}

	[Rpc.Host]
	public void ScaleEntityHost( GameObject entityGameObject, Vector3 scaleValues )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:entity:scale", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !entityGameObject.IsValid() )
		{
			return;
		}

		// Check for scalable entity or construct (unified - all require scalable tag)
		var entity = entityGameObject.GetComponent<BaseEntity>();

		if ( !entity.IsValid() )
		{
			player.Error( "#notify.scale.invalid_target" );
			return;
		}

		// Check permissions
		var canScale = entity.CanScale( player );
		if ( !canScale )
		{
			player.Error( "#notify.scale.invalid_target" );
			return;
		}

		// Server-side scale validation
		if ( scaleValues.x < PropDefinition.MinPropScale || scaleValues.x > PropDefinition.MaxPropScale ||
		     scaleValues.y < PropDefinition.MinPropScale || scaleValues.y > PropDefinition.MaxPropScale ||
		     scaleValues.z < PropDefinition.MinPropScale || scaleValues.z > PropDefinition.MaxPropScale )
		{
			player.Error( "#notify.scale.invalid_target" );
			return;
		}

		// Apply the scaling directly - unified approach for all
		entity.ApplyScaleOwner( scaleValues );
	}

	public void EjectToWaitingRoom()
	{
		WaitingRoom.PreviousServerName = Networking.ServerName;

		Networking.Disconnect();

		var sceneOptions = new SceneLoadOptions
		{
			ShowLoadingScreen = true, DeleteEverything = true
		};
		sceneOptions.SetScene( "scenes/waiting_room.scene" );

		Sandbox.Game.ChangeScene( sceneOptions );
	}

	public static string ModerateText( long user, string context, string text, bool isChat = false )
	{
		if ( !Config.Current.Game.ModerateText )
		{
			return text;
		}

		var result = text;
		if ( Config.Current.Game.TextSteamFilter )
		{
			result = isChat ? Steam.FilterChat( result ) : Steam.FilterText( result );
		}
		
		result = Utilities.WordFilter.Filter( result );

		if ( Networking.IsHost )
		{
			var isFlagged = !string.Equals( result, text, StringComparison.Ordinal );
			if ( isFlagged )
			{
				_ = ServerApiClient.SanctionPlayer( user, new CreateSanctionDto
				{
					Type = SanctionType.Automatic, Reason = $"[{context}] {text}", Notes = "Inappropriate language flagged by steam"
				} );
			}
		}

		return result;
	}
}
