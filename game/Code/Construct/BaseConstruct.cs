using System.Threading;
using System.Threading.Tasks;

public abstract partial class BaseConstruct : Component, IConstruct, IGameEvents, IOcclusionEvents
{
	public ConstructType Type { get; }

	[Property]
	[ReadOnly]
	[Group( "Construct" )]
	public long Owner { get; set; }

	[Property]
	[Group( "Construct" )]
	// ReSharper disable once MemberCanBePrivate.Global
	public string DataJson { get; set; } = string.Empty;

	public bool IsPreview { get; set; }
	public bool IsFrozen { get; private set; } = true;

	protected bool IsOwner => Player.Local.IsValid() && Owner == Player.Local.SteamId;

	public IConstructData Data { get; private set; } = new EmptyConstructData();

	[Property]
	protected Collider? Collider { get; set; }

	private CancellationTokenSource? _freezeCancellationTokenSource;


	// ReSharper disable once ConvertToPrimaryConstructor
	protected BaseConstruct( ConstructType type )
	{
		Type = type;
	}

	protected override void OnStart()
	{
		_targetPosition = WorldPosition;
		_targetRotation = WorldRotation;

		OcclusionSystem.Current.ForceCheckGameObject( GameObject );

		SetDataInternal( DataJson );

		ClearNetworkState();
	}

	protected override void OnUpdate()
	{
		OnUpdateNetworking();
		OnUpdateInterpolation();
	}

	public virtual void OnSecondlyUpdate()
	{
		OnSecondlyUpdateInterpolationState();
	}

	public virtual void OnOcclusionChanged( bool occlude )
	{
		if ( occlude )
		{
			ResetInterpolation();
		}
	}

	public virtual void OnOccluded() {}

	public virtual void OnUnoccluded() {}

	protected override void OnDestroy()
	{
		if ( Networking.IsHost && !IsPreview )
		{
			// Remove undo record and decrement construct count
			if ( Owner != 0 )
			{
				Undo.Current?.RemoveUndoById( Owner, Id );
				Construct.Current?.DecrementCount( Owner, Type );
			}

			Construct.Current?.BroadcastDestroy( GameObject );
		}

		// Clean up preview object
		if ( IsPreview )
		{
			GameObject.Destroy();
		}

		base.OnDestroy();
	}

	public void SetData( string jsonData )
	{
		SetDataInternal( jsonData );
	}

	private void SetDataInternal( string jsonData )
	{
		var definition = Construct.Current.GetDefinition( Type );
		if ( definition == null )
		{
			return;
		}

		var oldData = Data;

		if ( string.IsNullOrEmpty( jsonData ) )
		{
			Data = TypeLibrary.Create<IConstructData>( definition.DataType ) ?? new EmptyConstructData();
		}
		else
		{
			var result = Construct.Current.Serializer.DeserializeWithMigration( jsonData, definition );
			Data = result.IsSuccess ? result.Value :
				TypeLibrary.Create<IConstructData>( definition.DataType ) ??
				new EmptyConstructData();
		}

		DataJson = jsonData;

		OnDataChanged( oldData, Data );
	}

	public void Initialize( long owner, bool isPreview = false )
	{
		Owner = owner;
		IsPreview = isPreview;

		GameObject.NetworkMode = isPreview ? NetworkMode.Never : NetworkMode.Snapshot;
		GameObject.Tags.Set( Constants.OccludableTag, !isPreview );
		GameObject.Tags.Set( Constants.OccludeTag, !IsPreview );

		// Ensure collider is bound correctly
		if ( !Collider.IsValid() )
		{
			Collider = GameObject.GetComponentsInChildren<Collider>().FirstOrDefault( x => !x.IsTrigger );
		}

		// Set collider to static if already frozen, otherwise delay
		if ( Collider.IsValid() && !Collider.Static && !isPreview )
		{
			if ( IsFrozen )
			{
				Collider.Static = true;
			}
			else
			{
				_freezeCancellationTokenSource?.Cancel();
				_freezeCancellationTokenSource = new CancellationTokenSource();
				_ = FreezeCollider( _freezeCancellationTokenSource.Token );
			}
		}
	}

	/// <summary>
	/// Called when Data property changes 
	/// </summary>
	protected virtual void OnDataChanged( IConstructData oldData, IConstructData newData ) {}

}
