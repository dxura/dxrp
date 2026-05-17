namespace Dxura.RP.Game;

/// <summary>
///     An ammo container. It holds ammo for a weapon.
/// </summary>
[Title( "Ammo" )]
[Group( "Weapon Components" )]
public class AmmoComponent : Component, IDroppedWeaponState<AmmoComponent>
{
	/// <summary>
	///     How much ammo are we holding?
	/// </summary>
	[Property]
	[Sync( SyncFlags.FromHost | SyncFlags.Query )]
	public int Ammo { get; set; } = 0;

	[Property] public int MaxAmmo { get; set; } = 30;

	/// <summary>
	///     Total reserve ammo the player has for this weapon
	/// </summary>
	[Property]
	[Sync( SyncFlags.FromHost | SyncFlags.Query )]
	public int ReserveAmmo { get; set; } = 90;

	/// <summary>
	///     Maximum reserve ammo the player can carry
	/// </summary>
	[Property]
	public int MaxReserveAmmo { get; set; } = 90;

	/// <summary>
	///     Do we have any ammo?
	/// </summary>
	[Property]
	public bool HasAmmo => Ammo > 0;

	/// <summary>
	///     Do we have any reserve ammo?
	/// </summary>
	public bool HasReserveAmmo => ReserveAmmo > 0;

	/// <summary>
	///     Is this container full?
	/// </summary>
	public bool IsFull => Ammo == MaxAmmo;

	public void CopyFrom( AmmoComponent other )
	{
		Ammo = other.Ammo;
		ReserveAmmo = other.ReserveAmmo;
	}

	/// <summary>
	/// Server‐only entry to pull bullets from reserve into the magazine.
	/// </summary>
	[Rpc.Host( NetFlags.HostOnly | NetFlags.Reliable )]
	public void HostReloadMagazine( int amountToLoad )
	{
		// Clamp to how much reserve we actually have
		var canTake = Math.Min( amountToLoad, ReserveAmmo );
		if ( canTake <= 0 )
		{
			return;
		}

		// Server‑authoritative updates
		ReserveAmmo -= canTake;
		Ammo = Math.Min( Ammo + canTake, MaxAmmo );

		// Broadcast the new values back to all clients
		UpdateAmmoValues( Ammo, ReserveAmmo );
	}

	/// <summary>
	/// Broadcast fresh ammo/reserve counts to every client.
	/// </summary>
	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void UpdateAmmoValues( int ammo, int reserveAmmo )
	{
		Ammo = ammo;
		ReserveAmmo = reserveAmmo;
	}
}
