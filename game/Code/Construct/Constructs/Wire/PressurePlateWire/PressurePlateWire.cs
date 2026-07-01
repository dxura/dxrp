namespace Dxura.RP.Game.Wire;

[Title( "Pressure Plate" )]
[Category( "Wire" )]
[Icon( "square_foot" )]
public class PressurePlateWire() : BaseWireConstruct( ConstructType.PressurePlateWire ), IWireEvents
{
	private PressurePlateWireData _data = new();

	private bool _hasBeenTriggeredSinceLastWireTick;
	private bool _wasOccupied;
	private bool _isPlateOccupied;
	private float _totalMassOnPlate;
	private float _animatedMassOnPlate;
	private float _lastBroadcastMass;
	private int _objectCountOnPlate;
	private Vector3 _plateRestPosition;

	[Property] public GameObject PlateModel { get; set; } = null!;
	[Property] public ModelRenderer PlateRenderer { get; set; } = null!;
	[Property] public BoxCollider PlateCollider { get; set; } = null!;

	[WireOutput( "triggered" )]
	public bool Triggered { get; set; }

	[WireOutput( "trigger_count" )]
	public float TriggerCount { get; set; }

	[WireOutput( "trigger_mass" )]
	public float TriggerMass { get; set; }

	[WireOutput( "object_count" )]
	public float ObjectCount { get; set; }

	[WireInput( "reset_count" )]
	public bool ResetCount
	{
		set
		{
			if ( value )
			{
				TriggerCount = 0f;
			}
		}
		get => false;
	}

	public override string Name => "Pressure Plate";

	public override Vector3 GetPortPosition()
	{
		return GameObject.WorldPosition + WorldRotation.Backward * (_data.Length * 0.5f);
	}

	protected override void OnStart()
	{
		EnsurePlateHierarchy();
		base.OnStart();
	}

	public override void OnUnoccluded()
	{
		base.OnUnoccluded();
		UpdateMeshes();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !GameManager.IsHeadless )
		{
			UpdatePlateAnimation();
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( Networking.IsHost && !IsPreview )
		{
			CheckZone();
		}
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as PressurePlateWireData ?? new PressurePlateWireData();
		EnsurePlateHierarchy();
		UpdateMeshes();
	}

	public void OnWireTick()
	{
		var wasTriggered = Triggered;
		var isCurrentlyTriggered = _isPlateOccupied;
		var hasBeenTriggered = _hasBeenTriggeredSinceLastWireTick || isCurrentlyTriggered;

		if ( !wasTriggered && hasBeenTriggered )
		{
			TriggerCount++;
		}

		TriggerMass = isCurrentlyTriggered ? _totalMassOnPlate : 0f;
		ObjectCount = isCurrentlyTriggered ? _objectCountOnPlate : 0f;

		if ( hasBeenTriggered != wasTriggered )
		{
			Triggered = hasBeenTriggered;
		}

		if ( isCurrentlyTriggered != _wasOccupied
		     || (isCurrentlyTriggered && Math.Abs( _totalMassOnPlate - _lastBroadcastMass ) > 0.5f)
		     || (!isCurrentlyTriggered && _lastBroadcastMass > 0f) )
		{
			_wasOccupied = isCurrentlyTriggered;
			_lastBroadcastMass = _totalMassOnPlate;
			BroadcastPlateState( _totalMassOnPlate );
		}

		_hasBeenTriggeredSinceLastWireTick = false;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastPlateState( float totalMass )
	{
		_animatedMassOnPlate = totalMass;
	}

	private void CheckZone()
	{
		var objects = FindObjectsInZone();
		var occupied = objects.Count > 0;
		var totalMass = 0f;
		foreach ( var obj in objects )
		{
			totalMass += GetObjectMass( obj );
		}

		if ( occupied && !_isPlateOccupied )
		{
			_hasBeenTriggeredSinceLastWireTick = true;
		}

		_isPlateOccupied = occupied;
		_totalMassOnPlate = totalMass;
		_objectCountOnPlate = objects.Count;
	}

	private List<GameObject> FindObjectsInZone()
	{
		GetPlateMetrics( out var halfLength, out var halfWidth, out var plateTop );
		var candidates = QueryPhysicsCandidates(
			BuildZoneBounds( plateTop - PressurePlateWireDefinition.SurfaceTolerance, plateTop + PressurePlateWireDefinition.StackSearchHeight )
		);

		var counted = new HashSet<GameObject>();
		foreach ( var root in candidates )
		{
			if ( PassesFilter( root ) && TouchesContactZone( root, halfLength, halfWidth, plateTop ) )
			{
				counted.Add( root );
			}
		}

		for ( var pass = 0; pass < PressurePlateWireDefinition.MaxStackPasses; pass++ )
		{
			var addedAny = false;

			foreach ( var root in candidates )
			{
				if ( counted.Contains( root )
				     || !PassesFilter( root )
				     || root.GetComponent<Player>().IsValid()
				     || !IsStackedOn( counted, root ) )
				{
					continue;
				}

				counted.Add( root );
				addedAny = true;
			}

			if ( !addedAny )
			{
				break;
			}
		}

		return counted.ToList();
	}

	private IEnumerable<GameObject> QueryPhysicsCandidates( BBox bounds )
	{
		var processed = new HashSet<GameObject>();

		foreach ( var hit in Scene.FindInPhysics( bounds ) )
		{
			var root = hit.Root;
			if ( !root.IsValid() || root == GameObject.Root || !processed.Add( root ) )
			{
				continue;
			}

			yield return root;
		}
	}

	private bool TouchesContactZone( GameObject root, float halfLength, float halfWidth, float plateTop )
	{
		if ( root.GetComponent<Player>().IsValid() )
		{
			return IsPointInContactZone( GetPlayerSamplePoint( root ), halfLength, halfWidth, plateTop );
		}

		var transform = GameObject.WorldTransform;
		var minZ = plateTop - PressurePlateWireDefinition.SurfaceTolerance;
		var maxZ = plateTop + PressurePlateWireDefinition.DetectionHeight;
		var foundCollider = false;

		foreach ( var collider in root.GetComponentsInChildren<Collider>( false ) )
		{
			if ( !collider.IsValid() )
			{
				continue;
			}

			foundCollider = true;

			if ( IntersectsZone( collider.GetWorldBounds(), transform, halfLength, halfWidth, minZ, maxZ ) )
			{
				return true;
			}
		}

		return !foundCollider && IsPointInContactZone( root.WorldPosition, halfLength, halfWidth, plateTop );
	}

	private bool IsStackedOn( HashSet<GameObject> supports, GameObject obj )
	{
		if ( !TryGetWorldBounds( obj, out var objBounds ) )
		{
			return false;
		}

		var transform = GameObject.WorldTransform;
		var objBottom = GetLocalZRange( objBounds, transform ).Min;

		foreach ( var support in supports )
		{
			if ( support == obj || !TryGetWorldBounds( support, out var supportBounds ) )
			{
				continue;
			}

			if ( !OverlapsHorizontally( objBounds, supportBounds, transform ) )
			{
				continue;
			}

			var gap = objBottom - GetLocalZRange( supportBounds, transform ).Max;
			if ( Math.Abs( gap ) <= PressurePlateWireDefinition.StackSupportGap )
			{
				return true;
			}
		}

		return false;
	}

	private bool IsPointInContactZone( Vector3 worldPoint, float halfLength, float halfWidth, float plateTop )
	{
		var localPoint = GameObject.WorldTransform.PointToLocal( worldPoint );

		if ( Math.Abs( localPoint.x ) > halfLength || Math.Abs( localPoint.y ) > halfWidth )
		{
			return false;
		}

		return localPoint.z >= plateTop - PressurePlateWireDefinition.SurfaceTolerance
		       && localPoint.z <= plateTop + PressurePlateWireDefinition.DetectionHeight;
	}

	private static Vector3 GetPlayerSamplePoint( GameObject root )
	{
		var player = root.GetComponent<Player>();
		if ( player.IsValid()
		     && player.Controller.IsValid()
		     && player.Controller.FeetCollider.IsValid() )
		{
			return player.Controller.FeetCollider.WorldPosition;
		}

		return root.WorldPosition;
	}

	private bool PassesFilter( GameObject root )
	{
		return _data.FilterType switch
		{
			TriggerFilterType.PlayerOnly => root.Tags.Has( Constants.PlayerTag ),
			TriggerFilterType.EntityOnly => root.Tags.Has( Constants.EntityTag ),
			TriggerFilterType.ConstructOnly => root.Tags.Has( Constants.ConstructTag ),
			_ => true
		};
	}

	private float GetPlateTopLocalZ()
	{
		var plateCenterZ = PlateModel.IsValid()
			? PlateModel.LocalPosition.z
			: _plateRestPosition.z;

		return plateCenterZ + _data.Depth * 0.5f;
	}

	private void GetPlateMetrics( out float halfLength, out float halfWidth, out float plateTop )
	{
		halfLength = _data.Length * 0.5f;
		halfWidth = _data.Width * 0.5f;
		plateTop = GetPlateTopLocalZ();
	}

	private BBox BuildZoneBounds( float minLocalZ, float maxLocalZ )
	{
		GetPlateMetrics( out var halfLength, out var halfWidth, out _ );
		return LocalFootprintToWorldBounds( halfLength, halfWidth, minLocalZ, maxLocalZ );
	}

	private BBox LocalFootprintToWorldBounds( float halfLength, float halfWidth, float minLocalZ, float maxLocalZ )
	{
		var transform = GameObject.WorldTransform;
		return BBox.FromPoints(
		[
			transform.PointToWorld( new Vector3( -halfLength, -halfWidth, minLocalZ ) ),
			transform.PointToWorld( new Vector3( halfLength, -halfWidth, minLocalZ ) ),
			transform.PointToWorld( new Vector3( -halfLength, halfWidth, minLocalZ ) ),
			transform.PointToWorld( new Vector3( halfLength, halfWidth, minLocalZ ) ),
			transform.PointToWorld( new Vector3( -halfLength, -halfWidth, maxLocalZ ) ),
			transform.PointToWorld( new Vector3( halfLength, -halfWidth, maxLocalZ ) ),
			transform.PointToWorld( new Vector3( -halfLength, halfWidth, maxLocalZ ) ),
			transform.PointToWorld( new Vector3( halfLength, halfWidth, maxLocalZ ) )
		] );
	}

	private static bool IntersectsZone(
		BBox worldBounds,
		Transform plateTransform,
		float halfLength,
		float halfWidth,
		float minZ,
		float maxZ )
	{
		var (minX, maxX, minY, maxY, localMinZ, localMaxZ) = GetLocalExtents( worldBounds, plateTransform );

		return maxX >= -halfLength && minX <= halfLength
		       && maxY >= -halfWidth && minY <= halfWidth
		       && localMaxZ >= minZ && localMinZ <= maxZ;
	}

	private static bool OverlapsHorizontally( BBox a, BBox b, Transform plateTransform )
	{
		var aExtents = GetLocalExtents( a, plateTransform );
		var bExtents = GetLocalExtents( b, plateTransform );

		return aExtents.MaxX >= bExtents.MinX && aExtents.MinX <= bExtents.MaxX
		       && aExtents.MaxY >= bExtents.MinY && aExtents.MinY <= bExtents.MaxY;
	}

	private static (float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ) GetLocalExtents(
		BBox worldBounds,
		Transform plateTransform )
	{
		var localMin = plateTransform.PointToLocal( worldBounds.Mins );
		var localMax = plateTransform.PointToLocal( worldBounds.Maxs );

		return (
			MathF.Min( localMin.x, localMax.x ),
			MathF.Max( localMin.x, localMax.x ),
			MathF.Min( localMin.y, localMax.y ),
			MathF.Max( localMin.y, localMax.y ),
			MathF.Min( localMin.z, localMax.z ),
			MathF.Max( localMin.z, localMax.z )
		);
	}

	private static (float Min, float Max) GetLocalZRange( BBox worldBounds, Transform plateTransform )
	{
		var extents = GetLocalExtents( worldBounds, plateTransform );
		return (extents.MinZ, extents.MaxZ);
	}

	private static bool TryGetWorldBounds( GameObject root, out BBox bounds )
	{
		bounds = default;
		BBox? combined = null;

		foreach ( var collider in root.GetComponentsInChildren<Collider>( false ) )
		{
			if ( !collider.IsValid() )
			{
				continue;
			}

			combined = combined.HasValue
				? combined.Value.AddBBox( collider.GetWorldBounds() )
				: collider.GetWorldBounds();
		}

		if ( !combined.HasValue )
		{
			return false;
		}

		bounds = combined.Value;
		return true;
	}

	private void UpdatePlateAnimation()
	{
		if ( !PlateModel.IsValid() || !PlateRenderer.IsValid() || GameObject.Tags.Has( Constants.OccludeTag ) )
		{
			return;
		}

		var maxPress = PressurePlateWireDefinition.GetMaxPressDepth( _data.Depth );
		var pressDepth = PressurePlateWireDefinition.GetPressDepthFromMass( _animatedMassOnPlate, _data.Depth );
		var targetPosition = _plateRestPosition + new Vector3( 0, 0, -pressDepth );
		var lerpSpeed = 14f * Time.Delta;

		PlateModel.LocalPosition = Vector3.Lerp( PlateModel.LocalPosition, targetPosition, lerpSpeed );

		var pressFactor = maxPress > 0f ? pressDepth / maxPress : 0f;
		PlateRenderer.Tint = Color.Lerp(
			PressurePlateWireDefinition.RestPlateColor,
			PressurePlateWireDefinition.PressedPlateColor,
			pressFactor
		);
	}

	private void EnsurePlateHierarchy()
	{
		if ( !PlateModel.IsValid() )
		{
			PlateModel = new GameObject( GameObject, true, "Plate" )
			{
				LocalPosition = Vector3.Zero,
				LocalRotation = Rotation.Identity,
				NetworkMode = NetworkMode.Never
			};
		}

		if ( !PlateRenderer.IsValid() )
		{
			PlateRenderer = PlateModel.Components.Create<ModelRenderer>();
			PlateRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;
		}

		if ( IsPreview )
		{
			PlateCollider?.Destroy();
			return;
		}

		if ( !PlateCollider.IsValid() )
		{
			PlateCollider = PlateModel.Components.Create<BoxCollider>();
		}
	}

	private void UpdateMeshes()
	{
		_plateRestPosition = new Vector3( 0, 0, _data.Depth * 0.5f - PressurePlateWireDefinition.FlatSpawnBuffer );

		if ( PlateRenderer.IsValid() )
		{
			var plateMesh = CreateBoxMesh( _data.Width, _data.Length, _data.Depth );
			PlateRenderer.Model = Model.Builder.AddMesh( plateMesh ).Create();

			var restColor = PressurePlateWireDefinition.RestPlateColor;
			PlateRenderer.Tint = IsPreview
				? new Color( restColor.r, restColor.g, restColor.b, 0.5f )
				: restColor;
		}

		if ( !IsPreview && PlateCollider.IsValid() )
		{
			PlateCollider.Scale = new Vector3( _data.Length, _data.Width, _data.Depth );
		}

		if ( PlateModel.IsValid() )
		{
			PlateModel.LocalPosition = _plateRestPosition;
		}
	}

	private static Mesh CreateBoxMesh( float width, float height, float thickness )
	{
		var halfWidth = width * 0.5f;
		var halfHeight = height * 0.5f;
		var halfThickness = thickness * 0.5f;

		var vertices = new Vertex[]
		{
			new() { Position = new Vector3( -halfHeight, -halfWidth, halfThickness ), Normal = Vector3.Forward },
			new() { Position = new Vector3( halfHeight, -halfWidth, halfThickness ), Normal = Vector3.Forward },
			new() { Position = new Vector3( halfHeight, halfWidth, halfThickness ), Normal = Vector3.Forward },
			new() { Position = new Vector3( -halfHeight, halfWidth, halfThickness ), Normal = Vector3.Forward },
			new() { Position = new Vector3( -halfHeight, -halfWidth, -halfThickness ), Normal = Vector3.Backward },
			new() { Position = new Vector3( halfHeight, -halfWidth, -halfThickness ), Normal = Vector3.Backward },
			new() { Position = new Vector3( halfHeight, halfWidth, -halfThickness ), Normal = Vector3.Backward },
			new() { Position = new Vector3( -halfHeight, halfWidth, -halfThickness ), Normal = Vector3.Backward }
		};

		var indices = new[]
		{
			0, 1, 2, 0, 2, 3,
			5, 4, 7, 5, 7, 6,
			4, 0, 3, 4, 3, 7,
			1, 5, 6, 1, 6, 2,
			3, 2, 6, 3, 6, 7,
			4, 5, 1, 4, 1, 0
		};

		var material = Material.Load( "materials/default.vmat" );
		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer( vertices.Length, vertices );
		mesh.CreateIndexBuffer( indices.Length, indices );
		mesh.Bounds = new BBox(
			new Vector3( -halfHeight, -halfWidth, -halfThickness ),
			new Vector3( halfHeight, halfWidth, halfThickness )
		);

		return mesh;
	}

	private static float GetObjectMass( GameObject obj )
	{
		var player = obj.GetComponent<Player>();
		if ( player.IsValid() && player.Controller.IsValid() )
		{
			return player.Controller.BodyMass;
		}

		var massFromModel = GetMassFromModel( obj );
		if ( massFromModel > 0f )
		{
			return massFromModel;
		}

		var construct = obj.GetComponent<IConstruct>();
		if ( construct is not { IsFrozen: true } )
		{
			var rigidbody = obj.GetComponentInChildren<Rigidbody>();
			if ( rigidbody.IsValid() && rigidbody.Mass > 0f )
			{
				return rigidbody.Mass;
			}
		}

		return GetMassFromLocalBounds( obj );
	}

	private static float GetMassFromModel( GameObject obj )
	{
		var modelRenderer = obj.GetComponentInChildren<ModelRenderer>( false );
		if ( modelRenderer.IsValid() && modelRenderer.Model.IsValid() && modelRenderer.Model.Bounds.Size.Length > 0.1f )
		{
			return MassFromVolume( modelRenderer.Model.Bounds.Size * obj.WorldScale );
		}

		var skinnedRenderer = obj.GetComponentInChildren<SkinnedModelRenderer>( false );
		if ( skinnedRenderer.IsValid() && skinnedRenderer.Model.IsValid() && skinnedRenderer.Model.Bounds.Size.Length > 0.1f )
		{
			return MassFromVolume( skinnedRenderer.Model.Bounds.Size * obj.WorldScale );
		}

		return 0f;
	}

	private static float GetMassFromLocalBounds( GameObject obj )
	{
		if ( !TryGetLocalBounds( obj, out var localBounds ) )
		{
			return 0f;
		}

		return MassFromVolume( localBounds.Size );
	}

	private static float MassFromVolume( Vector3 size )
	{
		var volume = size.x * size.y * size.z;
		if ( volume <= 0f )
		{
			return 0f;
		}

		return Math.Clamp(
			volume * PressurePlateWireDefinition.MassPerVolumeUnit,
			1f,
			PressurePlateWireDefinition.ReferenceMass * 2f
		);
	}

	private static bool TryGetLocalBounds( GameObject root, out BBox bounds )
	{
		bounds = default;
		var transform = root.WorldTransform;
		BBox? combined = null;

		foreach ( var collider in root.GetComponentsInChildren<Collider>( false ) )
		{
			if ( !collider.IsValid() || collider.IsTrigger )
			{
				continue;
			}

			ExpandLocalBounds( ref combined, collider.GetWorldBounds(), transform );
		}

		if ( !combined.HasValue || combined.Value.Size.Length <= 0.01f )
		{
			return false;
		}

		bounds = combined.Value;
		return true;
	}

	private static void ExpandLocalBounds( ref BBox? combined, BBox worldBounds, Transform objectTransform )
	{
		var mins = worldBounds.Mins;
		var maxs = worldBounds.Maxs;

		AddLocalPoint( ref combined, objectTransform.PointToLocal( new Vector3( mins.x, mins.y, mins.z ) ) );
		AddLocalPoint( ref combined, objectTransform.PointToLocal( new Vector3( maxs.x, mins.y, mins.z ) ) );
		AddLocalPoint( ref combined, objectTransform.PointToLocal( new Vector3( mins.x, maxs.y, mins.z ) ) );
		AddLocalPoint( ref combined, objectTransform.PointToLocal( new Vector3( maxs.x, maxs.y, mins.z ) ) );
		AddLocalPoint( ref combined, objectTransform.PointToLocal( new Vector3( mins.x, mins.y, maxs.z ) ) );
		AddLocalPoint( ref combined, objectTransform.PointToLocal( new Vector3( maxs.x, mins.y, maxs.z ) ) );
		AddLocalPoint( ref combined, objectTransform.PointToLocal( new Vector3( mins.x, maxs.y, maxs.z ) ) );
		AddLocalPoint( ref combined, objectTransform.PointToLocal( new Vector3( maxs.x, maxs.y, maxs.z ) ) );
	}

	private static void AddLocalPoint( ref BBox? combined, Vector3 localPoint )
	{
		combined = combined.HasValue
			? combined.Value.AddPoint( localPoint )
			: new BBox( localPoint, localPoint );
	}
}
