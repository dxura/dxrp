namespace Dxura.RP.Game;

public class Snapshot
{
	public ConstructDupe? WorldDupe { get; set; }
	public List<PlayerSnapshotData> Players { get; set; } = new();

	// Game objects are stored as whole, mostly for entities with too much state.
	public List<string> GameObjects { get; set; } = new();

	public List<DoorSnapshotData> Doors { get; set; } = new();

}
