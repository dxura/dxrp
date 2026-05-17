namespace Dxura.RP.Game;

public sealed class MapFitter : SingletonComponent<MapFitter>
{
	[Property]
	public required MapInstance MapInstance { get; set; }

	[Property]
	public required Dictionary<string, GameObject> FittedMapPrefabs { get; set; }

	[Property]
	public required GameObject GlassPrefab { get; set; }

	private GameObject? _currentFitting;
	private bool _currentFittingIsSceneObject;
	private bool _fittedViaApi;

	protected override void OnStart()
	{
		if ( !TryResolveMapInstance() )
		{
			return;
		}

		MapInstance.OnMapLoaded += OnMapLoaded;

		if ( !ServerApiLink.HasAuthorizationKey && MapInstance.IsLoaded )
		{
			OnMapLoaded();
		}
	}

	/// <summary>
	/// Fits a map by identifier. When called from the API, an optional prefab override can be provided.
	/// When no prefab is provided, the fitting falls back to the FittedMapPrefabs dictionary.
	/// </summary>
	public static void Fit( string? mapIdent, GameObject? fitting = null )
	{
		if ( !Instance.IsValid() )
		{
			Log.Warning( "[Fitter] Cannot fit map - MapFitter instance not found" );
			IGameEvents.Post( x => x.OnMapFitted() );
			return;
		}

		if ( !Instance.TryResolveMapInstance() )
		{
			Log.Warning( "[Fitter] Cannot fit map - MapInstance not found" );
			IGameEvents.Post( x => x.OnMapFitted() );
			return;
		}

		Instance._currentFitting = fitting;
		Instance._currentFittingIsSceneObject = fitting != null;
		Instance._fittedViaApi = true;

		var alreadyLoaded = Instance.MapInstance.IsValid() && Instance.MapInstance.MapName == mapIdent && Instance.MapInstance.IsLoaded;
		Instance.MapInstance.MapName = mapIdent;

		Log.Info( $"[Fitter] Map set to '{mapIdent}' with fitting '{fitting?.Name ?? "null"}'" );

		if ( alreadyLoaded )
		{
			Instance.OnMapLoaded();
		}
	}

	private void OnMapLoaded()
	{
		if ( !TryResolveMapInstance() )
		{
			return;
		}

		if ( !Networking.IsHost || !Config.Current.Game.MapFittingEnabled )
		{
			return;
		}

		// When a server API key is provided, skip the initial automatic map load —
		// fitting will be triggered explicitly via Fit() from the API.
		if ( ServerApiLink.HasAuthorizationKey && !_fittedViaApi )
		{
			return;
		}

		Sentinel.Sentinel.Current?.CurrentMap = null;

		var map = MapInstance.MapName;

		if ( string.IsNullOrEmpty( map ) )
		{
			Log.Warning( "[Fitter] Map is not provided, skipping fitting" );
			return;
		}

		Log.Info( $"[Fitter] Fitting {map}" );

		if ( !MapInstance.IsValid() )
		{
			Log.Error( "[Fitter] Failed to fit map" );
			return;
		}

		// Use explicitly provided fitting, or fall back to the prefab dictionary
		_currentFitting ??= FittedMapPrefabs.GetValueOrDefault( map );

		if ( _currentFitting == null )
		{
			FitMarkers();
			IGameEvents.Post( x => x.OnMapFitted() );
			Sentinel.Sentinel.Current?.CurrentMap = MapInstance;
			Log.Warning( $"[Fitter] No fitting prefab defined for map '{map}'" );
			return;
		}

		ApplyPrefab( _currentFitting );
	}

	/// <summary>
	/// Applies a fitting to the scene, cloning asset prefabs when needed before
	/// spawning children, fitting markers, and firing events.
	/// </summary>
	private void ApplyPrefab( GameObject gameObject )
	{
		if ( !gameObject.IsValid() || !Config.Current.Game.MapFittingEnabled )
		{
			return;
		}

		if ( !_currentFittingIsSceneObject )
		{
			// Asset prefabs need cloning before their children can be re-parented into the map scene.
			gameObject = gameObject.Clone();
		}

		gameObject.NetworkSpawn();

		foreach ( var child in gameObject.Children.ToArray() )
		{
			MoveToRoot( child );
		}

		// Remove the top-level prefab since we only want its children
		gameObject.Destroy();

		FitMarkers();

		// Refresh all systems that depend on the map fitting (via event)
		IGameEvents.Post( x => x.OnMapFitted() );

		_currentFitting = null;
		_currentFittingIsSceneObject = false;
		Sentinel.Sentinel.Current?.CurrentMap = MapInstance;
	}

	private bool TryResolveMapInstance()
	{
		if ( MapInstance is not null && MapInstance.IsValid() )
		{
			return true;
		}

		MapInstance = Scene.Components.GetAll<MapInstance>( FindMode.EverythingInSelfAndDescendants ).FirstOrDefault()!;
		if ( MapInstance is not null && MapInstance.IsValid() )
		{
			return true;
		}

		Log.Error( "[Fitter] MapInstance not found" );
		return false;
	}

	private void MoveToRoot( GameObject obj )
	{
		if ( obj.NetworkMode == NetworkMode.Object || obj.Tags.Contains( Constants.MapTag ) )
		{
			obj.Parent = Scene;
			return;
		}

		foreach ( var child in obj.Children.ToArray() )
		{
			MoveToRoot( child );
		}
	}

	private void FitMarkers()
	{
		// Fill glass panes - collect placeholders first to avoid collection modification during enumeration
		var glassMarkers = Scene.FindAllWithTag( "glass_marker" ).ToArray();
		foreach ( var glassPlaceholder in glassMarkers )
		{
			var glass = GlassPrefab.Clone();
			glass.WorldPosition = glassPlaceholder.WorldPosition;
			glass.WorldRotation = glassPlaceholder.WorldRotation;

			var glassComponent = glass.GetComponent<Glass>();
			if ( !glassComponent.IsValid() )
			{
				continue;
			}

			var localBounds = glassPlaceholder.GetLocalBounds();
			var scale = glassPlaceholder.WorldScale;

			// Apply scale to bounds and create points in local 2D space (XY plane)
			var scaledMins = localBounds.Mins * scale;
			var scaledMaxs = localBounds.Maxs * scale;

			glassComponent.Points =
			[
				new Vector2( scaledMins.x, scaledMins.y ),
				new Vector2( scaledMaxs.x, scaledMins.y ),
				new Vector2( scaledMaxs.x, scaledMaxs.y ),
				new Vector2( scaledMins.x, scaledMaxs.y )
			];

			glassPlaceholder.Destroy();
		}
	}
}
