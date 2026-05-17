namespace Dxura.RP.Game;

public interface IConstruct : IOwned
{
	public Guid Id { get; }
	public ConstructType Type { get; }

	public Guid NetworkOwner { get; }

	public GameObject GameObject { get; }

	public IConstructData Data { get; }

	public void Initialize( long owner, bool isPreview = false );

	public bool IsValid()
	{
		return GameObject.IsValid();
	}

	public bool IsPreview { get; }
	public bool IsFrozen { get; }

	public void BroadcastData( string dataJson );
	public void SetData( string dataJson );

	public void Freeze( Vector3 position, Rotation rotation );
	public void Unfreeze();

	public bool RequestNetworkOwnership();

	public void Destroy();

}

public interface IWireConstruct : IConstruct
{
	public string Name { get; }
}
