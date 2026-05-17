namespace Dxura.RP.Game;

public sealed partial class Glass : Component, Component.IDamageable, IGameEvents
{
	[Property] [MakeDirty] public Material Material { get; set; } = null!;
	[Property] [MakeDirty] public Surface Surface { get; set; } = null!;
	[Property] [MakeDirty] public float Thickness { get; set; } = 1;
	[Property] [MakeDirty] public Vector3 TextureAxisU { get; set; } = Vector3.Forward;
	[Property] [MakeDirty] public Vector3 TextureAxisV { get; set; } = Vector3.Right;
	[Property] [MakeDirty] public Vector2 TextureScale { get; set; } = 1;
	[Property] [MakeDirty] public Vector2 TextureOffset { get; set; } = 0;
	[Property] [MakeDirty] public Vector2 TextureSize { get; set; } = 512;
	[Property] public required List<Vector2> Points { get; set; } = [];
	[Property] public float ShardLifeTime { get; set; } = 6.0f;

	private readonly Dictionary<PhysicsShape, Shard> _shards = new();
	private readonly List<PhysicsShape> _shardsToRemove = new();

	private TimeSince _lastShard = 0;

	protected override void DrawGizmos()
	{
		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.Color = Color.Red;

		// Center
		Gizmo.Draw.LineSphere( Vector3.Zero, 2 );


		var points = new List<Vector3>();
		foreach ( var point in Points )
		{
			var p = new Vector3( point.x, point.y, 0 );
			Gizmo.Draw.LineSphere( p, 1 );
			points.Add( p );
		}
		Gizmo.Draw.Color = Color.Orange;

		Gizmo.Draw.IgnoreDepth = false;
		Gizmo.Draw.SolidBox( BBox.FromPoints( points ) );
		Gizmo.Hitbox.BBox( BBox.FromPoints( points, 5f ) );
	}

	protected override void OnStart()
	{
		CreatePrimaryShard();
	}

	protected override void OnUpdate()
	{
		if ( _lastShard > ShardLifeTime * 2 || GameManager.IsHeadless )
		{
			return;
		}

		foreach ( var shard in _shards.Values )
		{
			if ( !shard.IsValid() )
			{
				continue;
			}

			var body = shard.PhysicsBody;
			if ( !body.IsValid() )
			{
				continue;
			}

			if ( !shard.IsLoose )
			{
				body.Transform = Transform.World;
			}

			shard.SceneObject.Transform = body.Transform;
		}
	}

	public void OnSecondlyUpdate()
	{
		if ( _lastShard > ShardLifeTime * 2 || GameManager.IsHeadless )
		{
			return;
		}

		foreach ( var (shape, shard) in _shards )
		{
			if ( shard.IsLoose && shard.TimeCreated > ShardLifeTime )
			{
				_shardsToRemove.Add( shape );
				shard.Destroy();
			}
		}

		foreach ( var shard in _shardsToRemove )
		{
			_shards.Remove( shard );
		}

		_shardsToRemove.Clear();
	}

	public void Repair( bool force = false )
	{
		// Don't repair if still primary shard or have been broke in the past minute
		if ( !force && (_shards.Count == 1 || _lastShard <= 60) )
		{
			return;
		}

		DestroyShards();
		CreatePrimaryShard();
	}
}
