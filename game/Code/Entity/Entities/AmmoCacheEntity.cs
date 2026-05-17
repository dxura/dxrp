using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Entities;

[Title( "Ammo Cache" )]
[Spawnable]
[Icon( "inventory_2" )]
[Category( "Entities" )]
public class AmmoCacheEntity : BaseEntity, Component.IPressable, IContextualObject
{
	[Property] public int AmmoAmount { get; set; } = 30;
	[Property] public SoundEvent? UseSound { get; set; }

	public override string DisplayName => base.DisplayName ?? string.Empty;

	// IContextualObject
	public Vector3 ContextPosition => WorldPosition + Vector3.Up * 10f;
	private bool IsActive => Governance.Current.IsUpgradeActive( Governance.PdUpgradeType.AmmoCache );

	public string InputHint => "use";
	public string DisplayText => IsActive ? "#entity.ammocache.refill" : "#entity.ammocache.inactive";
	public float ContextMaxDistance => Config.Current.Game.ReachDistance;
	public bool LookOpacity => false;

	public bool Press( IPressable.Event e )
	{
		if ( !IsActive )
		{
			Notify.Error( "#entity.ammocache.not_active" );
			return false;
		}

		if ( Player.Local.IsValid() && !Player.Local.Job.IsGovernmentRole() )
		{
			Notify.Error( "#generic.forbidden" );
			return false;
		}

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
		if ( !Governance.Current.IsUpgradeActive( Governance.PdUpgradeType.AmmoCache ) )
		{
			return;
		}

		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:action:quick", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() || !player.Job.IsGovernmentRole() )
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
			player.Error( "#entity.ammocache.hold_weapon" );
			return;
		}

		var reserveSpace = ammoComponent.MaxReserveAmmo - ammoComponent.ReserveAmmo;
		var ammoToAdd = Math.Min( AmmoAmount, reserveSpace );

		if ( ammoToAdd <= 0 )
		{
			return;
		}

		var newReserve = ammoComponent.ReserveAmmo + ammoToAdd;
		ammoComponent.UpdateAmmoValues( ammoComponent.Ammo, newReserve );

		UseSound?.Broadcast( WorldPosition );
	}
}
