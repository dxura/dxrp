namespace Dxura.RP.Game;

public abstract class SingletonComponent<T> : Component, IHotloadManaged
	where T : SingletonComponent<T>
{
	[SkipHotload]
	public static T Instance { get; private set; } = null!;

	void IHotloadManaged.Destroyed( Dictionary<string, object> state )
	{
		state["IsActive"] = Instance == this;
	}

	void IHotloadManaged.Created( IReadOnlyDictionary<string, object> state )
	{
		if ( state.GetValueOrDefault( "IsActive" ) is true )
		{
			Instance = (T)this;
		}
	}

	protected override void OnAwake()
	{
		if ( Active )
		{
			Instance = (T)this;
		}
	}

	protected override void OnDestroy()
	{
		if ( Instance == this )
		{
			Instance = null!;
		}
	}
}
