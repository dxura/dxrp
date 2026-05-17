namespace Dxura.RP.Game;

public class PlayerSnapshotData : SnapshotData
{
	public long SteamId { get; set; }

	public Vector3 Position { get; set; }

	public uint WalletBalance { get; set; }
	public string? JobPath { get; set; }
	public float Health { get; set; }

	public List<EquipmentSnapshotData> Equipment { get; set; } = new();
}

public class EquipmentSnapshotData
{
	public string ResourcePath { get; set; } = string.Empty;
	public int Ammo { get; set; }
	public int ReserveAmmo { get; set; }
}
