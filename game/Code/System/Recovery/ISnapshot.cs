namespace Dxura.RP.Game;

public enum SnapshotType
{
	GameObject,
	Player
}

public interface ISnapshot
{
	SnapshotType SnapshotType { get; }

	GameObject GameObject { get; }

	SnapshotData? Save()
	{
		return null;
	}
	void Load( SnapshotData data ) {}
}
