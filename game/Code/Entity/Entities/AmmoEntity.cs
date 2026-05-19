namespace Dxura.RP.Game.Entities;

[Title( "Ammo" )]
[Spawnable]
[Icon( "inventory_2" )]
[Category( "Entities" )]
public class AmmoEntity : BaseEntity, Component.IPressable
{
	[Property] public int AmmoAmount { get; set; } = 30;
	[Property] public bool IsInfinite { get; set; } = false;

	[Property]
	[Sync( SyncFlags.FromHost )]
	public int UsesRemaining { get; set; } = 1;

	public override string DisplayName => Language.GetPhrase( "entity.ammo" );

	protected override void OnStart()
	{
		base.OnStart();
	}

	public bool Press( IPressable.Event e )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "action:quick", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return false;
		}

		DoPickupHost();
		return true;
	}

	[Rpc.Host]
	private void DoPickupHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:action:quick", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		// Proximity and LOS check
		var tr = Scene.Trace.Ray( player.AimRay, Config.Current.Game.ReachDistance )
			.IgnoreGameObjectHierarchy( player.GameObject )
			.UseHitboxes()
			.Run();
		if ( !tr.Hit || tr.GameObject.Root != GameObject.Root )
		{
			return;
		}

		var currentWeapon = player.CurrentEquipment;
		if ( !currentWeapon.IsValid() )
		{
			return;
		}

		var ammoComponent = currentWeapon.Components.Get<AmmoComponent>( FindMode.EverythingInSelfAndDescendants );
		if ( ammoComponent == null )
		{
			player.Error( "#entity.ammo.hold_weapon" );
			return;
		}

		// Calculate how much reserve ammo can be added
		var reserveSpace = ammoComponent.MaxReserveAmmo - ammoComponent.ReserveAmmo;
		var ammoToAdd = Math.Min( AmmoAmount, reserveSpace );

		if ( ammoToAdd <= 0 )
		{
			return;
		}

		var newReserve = ammoComponent.ReserveAmmo + ammoToAdd;

		// Update ammo values
		ammoComponent.UpdateAmmoValues( ammoComponent.Ammo, newReserve );

		GameManager.Instance.AmmoSound?.Broadcast( WorldPosition );

		if ( IsInfinite )
		{
			return;
		}

		UsesRemaining--;
		if ( UsesRemaining <= 0 )
		{
			GameObject.Destroy();
		}
	}
}
