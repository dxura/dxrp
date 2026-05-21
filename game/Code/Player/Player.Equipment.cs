using Dxura.RP.Game.Equipments;
using Dxura.RP.Shared;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public partial class Player
{
	public enum PickupResult
	{
		None,
		Pickup,
		Refill
	}

	/// <summary>
	///     What weapon are we using?
	/// </summary>
	[Property]
	[Group( "State" )]
	[ReadOnly]
	public Equipment? CurrentEquipment { get; private set; }

	/// <summary>
	///     What equipment do we have right now?
	/// </summary>
	public IEnumerable<Equipment> Equipment => Components.GetAll<Equipment>( FindMode.EverythingInSelfAndDescendants );

	/// <summary>
	///     A <see cref="GameObject" /> that will hold all of our equipment.
	/// </summary>
	[Property]
	[Feature( "Misc" )]
	[Group( "Equipment" )]
	public GameObject WeaponGameObject { get; set; } = null!;

	public GameObject ViewModelGameObject => Scene.Camera.GameObject;

	/// <summary>
	///     How inaccurate are things like gunshots?
	/// </summary>
	public float Spread { get; set; }

	public TimeSince TimeSinceWeaponDeployed { get; private set; }

	public bool CantSwitch = false;


	public void OnEquipmentDeployed( Equipment equipment )
	{
		CurrentEquipment = equipment;
	}

	public void OnEquipmentHolstered( Equipment equipment )
	{
		if ( equipment == CurrentEquipment )
		{
			CurrentEquipment = null;
		}
	}

	public void EquipDefaultLoadoutHost()
	{
		Assert.True( Networking.IsHost );

		// Don't give equipment if restricted (only hands)
		if ( Restricted )
		{
			GiveHost( GameModeEquipments.Hands, true, false );
			return;
		}

		var isFirst = true;

		// Give default equipment (Citizen)
		if ( Job.IncludeDefaultEquipment )
		{
			var defaults = ResolveDefaultEquipment().ToList();
			
			foreach ( var equipmentResource in defaults )
			{
				GiveHost( equipmentResource, isFirst, false );
				isFirst = false;
			}
		}
		else
		{
			// Always give hands
			GiveHost( GameModeEquipments.Hands, isFirst, false );
			isFirst = false;
		}

		foreach ( var equipmentResource in ResolveJobEquipment( Job ) )
		{
			GiveHost( equipmentResource, isFirst, false );
		}
	}
	
	private static IEnumerable<GameModeEquipmentDto> ResolveDefaultEquipment()
	{
		var ids = Config.Current.GameMode.DefaultEquipmentIds;
		foreach ( var id in ids )
		{
			var equipment = GameModeEquipments.FindById( id );
			if ( equipment != null )
			{
				yield return equipment;
			}
		}
	}

	private static IEnumerable<GameModeEquipmentDto> ResolveJobEquipment( GameModeJobDto job )
	{
		foreach ( var id in job.GameModeEquipmentIds )
		{
			var equipment = GameModeEquipments.FindById( id );
			if ( equipment != null )
			{
				yield return equipment;
			}
		}
	}

	private void OnUpdateEquipmentSpread()
	{
		if ( HealthComponent.State != LifeState.Alive )
		{
			return;
		}

		var isAiming = CurrentEquipment.IsValid() && CurrentEquipment.Tags.Has( "aiming" );

		var config = Config.Current.Game;
		var spread = config.BaseSpreadAmount;
		var scale = config.VelocitySpreadScale;
		if ( isAiming )
		{
			spread *= config.AimSpread;
		}

		if ( isAiming )
		{
			scale *= config.AimVelocitySpreadScale;
		}

		var velLen = Controller.Velocity.Length;
		spread += velLen.Remap( 0, config.SpreadVelocityLimit, 0, 1, true ) * scale;

		if ( Controller.IsDucking && Controller.IsOnGround )
		{
			spread *= config.CrouchSpreadScale;
		}

		if ( !Controller.IsOnGround )
		{
			spread *= config.AirSpreadScale;
		}

		Spread = spread;
	}

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	private void SetCurrentWeaponOwner( Equipment? equipment )
	{
		SetCurrentEquipment( equipment );
	}

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	private void ClearCurrentWeaponHost()
	{
		if ( CurrentEquipment.IsValid() )
		{
			CurrentEquipment?.Holster();
		}
	}

	public void Holster()
	{
		if ( !IsLocalPlayer )
		{
			if ( Networking.IsHost )
			{
				ClearCurrentWeaponHost();
			}

			return;
		}

		CurrentEquipment?.Holster();
	}

	public void SetCurrentEquipment( Equipment? weapon )
	{
		if ( weapon == CurrentEquipment )
		{
			return;
		}

		ClearCurrentWeaponHost();

		// If this is the local player, deploy the weapon directly.
		if ( IsLocalPlayer )
		{
			TimeSinceWeaponDeployed = 0;
			weapon?.Deploy();
			return;
		}

		// If we're on the host, we need to handle both server-owned players (NPCs/fakes/DCs) and proxies for remote clients.
		if ( !Networking.IsHost )
		{
			return;
		}

		// If the player has a valid connection, it's a proxy for a remote client.
		// Send an RPC to that client to update their equipment.
		if ( Connection != null )
		{
			SetCurrentWeaponOwner( weapon );
		}
		// If there's no connection, it's a server-owned entity.
		// Deploy the weapon directly on the server.
		else
		{
			TimeSinceWeaponDeployed = 0;
			weapon?.Deploy();
		}
	}

	private void ClearViewModel()
	{
		foreach ( var weapon in Equipment )
		{
			weapon.ClearViewModel();
		}
	}

	private void CreateViewModel( bool playDeployEffects = true )
	{
		if ( !Controller.IsValid() || Controller.ThirdPerson )
		{
			return;
		}

		var weapon = CurrentEquipment;
		if ( weapon.IsValid() )
		{
			weapon.CreateViewModel( playDeployEffects );
		}
	}

	public void ClearLoadoutHost()
	{
		Assert.True( Networking.IsHost );

		HolsterCurrent();

		foreach ( var wpn in Equipment )
		{
			wpn.Enabled = false;
			wpn.GameObject.Destroy();
		}
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	public void RefillAmmoHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:ammo:refill", Config.Current.Game.RefillAmmoCooldown ) )
		{
			return;
		}

		foreach ( var wpn in Equipment )
		{
			if ( wpn.Components.Get<AmmoComponent>( FindMode.EnabledInSelfAndDescendants ) is {} ammo )
			{
				ammo.Ammo = ammo.MaxAmmo;
			}
		}
	}

	/// <summary>
	///     Try to drop the given held equipment item.
	/// </summary>
	/// <param name="weapon">Item to drop.</param>
	/// <param name="forceRemove">If we can't drop, remove it from the inventory anyway.</param>
	private void Drop( Equipment weapon, bool forceRemove = false )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "equipment:drop", Config.Current.Game.EquipmentDropCooldown, true ) )
		{
			return;
		}

		DropHost( weapon, forceRemove );
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	public void DropHost( Equipment weapon, bool forceRemove )
	{
		var callerId = Rpc.CallerId;
		Assert.True( Networking.IsHost );

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:equipment:drop", Config.Current.Game.EquipmentDropCooldown, true ) )
		{
			return;
		}

		if ( !weapon.IsValid() )
		{
			return;
		}

		// Make sure this player actually owns it
		if ( !Equipment.Contains( weapon ) )
		{
			return;
		}

		if ( weapon.CanDrop )
		{
			var resource = weapon.Resource;
			if ( resource == null )
			{
				return;
			}

			var tr = Scene.Trace.Ray( new Ray( AimRay.Position, AimRay.Forward ), 128 )
				.IgnoreGameObjectHierarchy( GameObject.Root )
				.WithoutTags( "trigger" )
				.Run();

			var worldModel = weapon.Resource.GetWorldModel();
			var position = tr.Hit && worldModel != null
				? tr.HitPosition + tr.Normal * worldModel.Bounds.Size.Length
				: AimRay.Position + AimRay.Forward * 32f;
			var rotation = Rotation.From( 0, Controller.EyeAngles.yaw + 90, 90 );

			var baseVelocity = Controller.Velocity;
			var droppedWeapon = DroppedEquipment.CreateHost( resource, position, rotation, weapon );

			if ( !tr.Hit )
			{
				droppedWeapon.Rigidbody.Velocity = baseVelocity + AimRay.Forward * 200.0f + Vector3.Up * 50;
				droppedWeapon.Rigidbody.AngularVelocity = Vector3.Random * 8.0f;
			}
		}

		if ( weapon.CanDrop || forceRemove )
		{
			RemoveEquipment( weapon );
		}
	}

	private void OnUpdateEquipment()
	{
		if ( Input.Pressed( "Drop" ) && CurrentEquipment.IsValid() )
		{
			Drop( CurrentEquipment );
			return;
		}

		if ( CantSwitch || _isFreeLooking || HasStatus( Constants.SurrenderStatus ) )
		{
			return;
		}

		foreach ( var slot in Enum.GetValues<EquipmentSlot>() )
		{
			if ( slot == EquipmentSlot.Undefined )
			{
				continue;
			}

			if ( !Input.Pressed( $"Slot{(int)slot}" ) )
			{
				continue;
			}

			SwitchToSlot( slot );
			return;
		}

		if ( Input.Pressed( "SlotPrev" ) )
		{
			SwitchToRelativeSlot( -1 );
			return;
		}

		if ( Input.Pressed( "SlotNext" ) )
		{
			SwitchToRelativeSlot( 1 );
			return;
		}
	}

	private void SwitchToRelativeSlot( int slotDelta )
	{
		var availableWeapons = Equipment.OrderBy( x => x.Resource.SlotValue() ).ToList();
		if ( availableWeapons.Count == 0 )
		{
			return;
		}

		var currentSlot = 0;
		for ( var index = 0; index < availableWeapons.Count; index++ )
		{
			var weapon = availableWeapons[index];
			if ( !weapon.IsDeployed )
			{
				continue;
			}

			currentSlot = index;
			break;
		}

		currentSlot += slotDelta;

		if ( currentSlot < 0 )
		{
			currentSlot = availableWeapons.Count - 1;
		}
		else if ( currentSlot >= availableWeapons.Count )
		{
			currentSlot = 0;
		}

		var weaponToSwitchTo = availableWeapons[currentSlot];
		if ( weaponToSwitchTo == CurrentEquipment )
		{
			return;
		}

		Switch( weaponToSwitchTo );
	}

	public void HolsterCurrent()
	{
		Assert.True( IsLocalPlayer || Networking.IsHost );
		SetCurrentEquipment( null );
	}

	public void SwitchToSlot( EquipmentSlot slot )
	{
		Assert.True( IsLocalPlayer || Networking.IsHost );

		var equipment = Equipment
			.Where( x => x.Resource.SlotValue() == slot )
			.ToArray();

		if ( equipment.Length == 0 )
		{
			return;
		}

		if ( equipment.Length == 1 && CurrentEquipment == equipment[0] )
		{
			HolsterCurrent();
			return;
		}

		var index = Array.IndexOf( equipment, CurrentEquipment );
		Switch( equipment[(index + 1) % equipment.Length] );
	}

	/// <summary>
	///     Tries to set the player's current weapon to a specific one, which has to be in the player's inventory.
	/// </summary>
	/// <param name="equipment"></param>
	public void Switch( Equipment? equipment )
	{
		Assert.True( IsLocalPlayer || Networking.IsHost );

		if ( !Equipment.Contains( equipment ) )
		{
			return;
		}

		SetCurrentEquipment( equipment );
	}

	/// <summary>
	///     Switches to bare hands when the player has them and switching is allowed (e.g. after holstering for an emote).
	/// </summary>
	public void SwitchToHandsHost()
	{
		Assert.True( Networking.IsHost );

		var hands = ResolveHandsEquipment();
		if ( !hands.IsValid() || CantSwitch )
		{
			return;
		}

		Switch( hands );

		// Holster/emote flow can deploy the same frame the camera leaves third person; rebuild the FP viewmodel once.
		if ( !IsLocalPlayer || !Controller.IsValid() || Controller.ThirdPerson || !CurrentEquipment.IsValid() )
		{
			return;
		}

		CurrentEquipment.CreateViewModel( false );
	}

	/// <summary>
	///     Removes the given weapon and destroys it.
	/// </summary>
	public void RemoveEquipment( Equipment equipment )
	{
		Assert.True( Networking.IsHost );

		if ( !Equipment.Contains( equipment ) )
		{
			return;
		}

		if ( CurrentEquipment == equipment )
		{
			var otherEquipment = Equipment.Where( x => x != equipment );
			var orderedBySlot = otherEquipment.OrderBy( x => x.Resource.SlotValue() );
			var targetWeapon = orderedBySlot.FirstOrDefault();

			if ( targetWeapon.IsValid() )
			{
				Switch( targetWeapon );
			}
		}

		equipment.GameObject.Destroy();
		equipment.Enabled = false;
	}

	/// <summary>
	///     Removes the given weapon (by its resource data) and destroys it.
	/// </summary>
	public void Remove( GameModeEquipmentDto resource )
	{
		var equipment = Equipment.FirstOrDefault( w => string.Equals( w.Identifier, resource.Identifier(), StringComparison.OrdinalIgnoreCase ) );
		if ( !equipment.IsValid() )
		{
			return;
		}

		RemoveEquipment( equipment );
	}

	public Equipment? GiveHost( GameModeEquipmentDto? resource, bool makeActive = true, bool canDrop = true )
	{
		Assert.True( Networking.IsHost );

		// If we're in charge, let's make some equipment.
		if ( resource == null )
		{
			Log.Warning( "A player loadout without a equipment? Nonsense." );
			return null;
		}

		var pickupResult = CanTake( resource );

		if ( pickupResult == PickupResult.None )
		{
			return null;
		}

		var existingEquipment = FindEquipment( resource );
		if ( existingEquipment.IsValid() )
		{
			if ( pickupResult != PickupResult.Refill )
			{
				return null;
			}

			return RefillEquipmentHost( existingEquipment ) ? existingEquipment : null;
		}

		if ( pickupResult == PickupResult.Refill )
		{
			var slotCurrent =
				Equipment.FirstOrDefault( equipment => equipment.Enabled && equipment.Resource.SlotValue() == resource.SlotValue() );
			if ( slotCurrent.IsValid() )
			{
				Drop( slotCurrent, true );
			}
		}

		var prefabPath = resource.PrefabPath();
		if ( string.IsNullOrWhiteSpace( prefabPath ) )
		{
			Log.Error( $"equipment doesn't have a prefab path? {resource.Identifier()}" );
			return null;
		}

		var prefab = GameObject.GetPrefab( prefabPath );
		if ( !prefab.IsValid() )
		{
			Log.Error( $"equipment prefab could not be loaded: {prefabPath}" );
			return null;
		}

		var gameObject = prefab.Clone( new CloneConfig
		{
			Transform = new Transform(), Parent = WeaponGameObject
		} );
		var component = gameObject.Components.Get<Equipment>( FindMode.EverythingInSelfAndDescendants );
		component.Identifier = resource.Identifier();
		component.OwnerId = Id;
		component.CanDrop = canDrop;
		gameObject.NetworkSpawn( Network.Owner );

		if ( makeActive && !CantSwitch )
		{
			SetCurrentEquipment( component );
		}

		return component;
	}

	public Equipment? FindEquipment( GameModeEquipmentDto resource )
	{
		return Equipment.FirstOrDefault( weapon => weapon.Enabled && string.Equals( weapon.Identifier, resource.Identifier(), StringComparison.OrdinalIgnoreCase ) );
	}

	/// <summary>
	///     Finds bare hands: <see cref="GameModeEquipments.Hands" /> lookup can miss when the content key differs (e.g. prefab name), so we also match <see cref="HandsEquipment" />.
	/// </summary>
	private Equipment? ResolveHandsEquipment()
	{
		var byHandsComponent = Equipment.FirstOrDefault( eq =>
			eq.Enabled &&
			eq.Components.Get<HandsEquipment>( FindMode.EverythingInSelfAndDescendants ).IsValid() );

		return byHandsComponent ?? FindEquipment( GameModeEquipments.Hands );
	}

	public bool CanPurchaseEquipment( GameModeEquipmentDto resource )
	{
		var pickupResult = CanTake( resource );
		if ( pickupResult == PickupResult.None )
		{
			return false;
		}

		if ( pickupResult != PickupResult.Refill )
		{
			return true;
		}

		var existingEquipment = FindEquipment( resource );
		if ( !existingEquipment.IsValid() )
		{
			return false;
		}

		var ammo = existingEquipment.Components.Get<AmmoComponent>( FindMode.EnabledInSelfAndDescendants );
		return ammo.IsValid() && (ammo.Ammo < ammo.MaxAmmo || ammo.ReserveAmmo < ammo.MaxReserveAmmo);
	}

	private static bool RefillEquipmentHost( Equipment equipment )
	{
		Assert.True( Networking.IsHost );

		var ammo = equipment.Components.Get<AmmoComponent>( FindMode.EnabledInSelfAndDescendants );
		if ( !ammo.IsValid() )
		{
			return false;
		}

		ammo.UpdateAmmoValues( ammo.MaxAmmo, ammo.MaxReserveAmmo );
		return true;
	}

	private void OnDeathEquipment()
	{
		if ( Config.Current.Game.DropWeaponOnDeath && !Restricted )
		{
			// 50% chance to drop only the currently held weapon
			if ( Random.Shared.NextSingle() < 0.5f && CurrentEquipment.IsValid() && CurrentEquipment.CanDrop )
			{
				DropEquipmentDirectly( CurrentEquipment );
			}
		}

		// Holster current weapon
		ClearCurrentWeaponHost();
	}

	private void DropAllDroppableEquipment()
	{
		var equipmentToDrop = Equipment.Where( x =>
			x.IsValid() &&
			x.CanDrop
		).ToList();

		foreach ( var equipment in equipmentToDrop )
		{
			DropEquipmentDirectly( equipment );
		}
	}

	private void DropEquipmentDirectly( Equipment equipment )
	{
		if ( !equipment.IsValid() )
		{
			return;
		}

		var tr = Scene.Trace.Ray( new Ray( AimRay.Position, AimRay.Forward ), 128 )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.WithoutTags( "trigger" )
			.Run();

		var resource = equipment.Resource;
		if ( resource == null )
		{
			RemoveEquipment( equipment );
			return;
		}

		var worldModel = equipment.Resource.GetWorldModel();
		var position = tr.Hit && worldModel != null
			? tr.HitPosition + tr.Normal * worldModel.Bounds.Size.Length
			: AimRay.Position + AimRay.Forward * 32f;
		var rotation = Rotation.From( 0, Controller.EyeAngles.yaw + 90, 90 );

		var baseVelocity = Controller.Velocity;
		var droppedWeapon = DroppedEquipment.CreateHost( resource, position, rotation, equipment );

		if ( !tr.Hit )
		{
			droppedWeapon.Rigidbody.Velocity = baseVelocity + AimRay.Forward * 200.0f + Vector3.Up * 50;
			droppedWeapon.Rigidbody.AngularVelocity = Vector3.Random * 8.0f;
		}

		RemoveEquipment( equipment );
	}

	public bool HasEquipment( GameModeEquipmentDto resource )
	{
		return FindEquipment( resource ).IsValid();
	}

	public bool HasInSlot( EquipmentSlot slot )
	{
		return Equipment.Any( weapon => weapon.Enabled && weapon.Resource.SlotValue() == slot );
	}

	public PickupResult CanTake( GameModeEquipmentDto resource )
	{
		// Political prisoners can't pick up weapons
		if ( Job.IsPoliticalPrisonerRole() )
		{
			return PickupResult.None;
		}
		
		if ( CantSwitch )
		{
			return PickupResult.None;
		}

		return HasEquipment( resource ) ? PickupResult.Refill : PickupResult.Pickup;
	}
}
