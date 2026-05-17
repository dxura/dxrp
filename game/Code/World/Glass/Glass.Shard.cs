namespace Dxura.RP.Game;

public sealed partial class Glass
{
	private Shard? CreateShard( Transform transform, List<Vector2> points )
	{
		if ( CalculatePathArea( points ) < 4.0f )
		{
			return null;
		}

		var hull = new List<Vector3>();
		var halfThickness = Thickness * 0.5f;

		foreach ( var point in points )
		{
			hull.Add( new Vector3( point.x, point.y, halfThickness ) );
			hull.Add( new Vector3( point.x, point.y, -halfThickness ) );
		}

		var body = new PhysicsBody( Scene.PhysicsWorld );
		var shape = body.AddHullShape( Vector3.Zero, Rotation.Identity, hull );
		shape.Tags.SetFrom( GameObject.Tags );
		shape.Surface = Surface;

		body.Component = this;
		body.EnableCollisionSounds = false;
		body.BodyType = PhysicsBodyType.Static;
		body.Transform = transform;

		var model = CreateModel( points );
		var sceneObject = new SceneObject( Scene.SceneWorld, model, transform );
		sceneObject.SetComponentSource( this );
		sceneObject.Tags.SetFrom( GameObject.Tags );
		sceneObject.Batchable = false;

		var shard = new Shard( sceneObject, shape, points.ToArray() );
		_shards.Add( shape, shard );

		return shard;
	}

	private void CreatePrimaryShard()
	{
		if ( Points is null || Points.Count < 3 )
		{
			Log.Warning( $"[Glass] Skipping shard creation for '{GameObject.Name}' because it does not have enough points." );
			return;
		}

		var points = IsPathClockwise( Points ) ? Points.Reverse<Vector2>().ToList() : Points.ToList();
		CreateShard( WorldTransform, points );
	}

	private void DestroyShard( Shard? shard )
	{
		if ( shard is null )
		{
			return;
		}

		_shards.Remove( shard.PhysicsShape );
		shard.Destroy();
	}

	private void DestroyShards()
	{
		foreach ( var shard in _shards.Values )
		{
			shard.Destroy();
		}

		_shards.Clear();
	}

	private sealed class Shard : IValid
	{
		public SceneObject SceneObject { get; }
		public PhysicsBody PhysicsBody { get; }
		public PhysicsShape PhysicsShape { get; }
		public Vector2[] Points { get; }
		public float Area { get; private set; }
		public bool IsLoose { get; set; }
		public TimeSince TimeCreated { get; }

		public Shard( SceneObject sceneObject, PhysicsShape shape, Vector2[] points )
		{
			SceneObject = sceneObject;
			PhysicsBody = shape.Body;
			PhysicsShape = shape;
			Points = points;
			TimeCreated = 0;
			Area = CalculateArea();
		}

		public bool IsValid => SceneObject.IsValid() && PhysicsBody.IsValid();

		public void Destroy()
		{
			SceneObject?.Delete();
			PhysicsBody?.Remove();
		}

		public bool IsPointInside( Vector2 point )
		{
			if ( Points.Length < 3 )
			{
				return false;
			}

			var positive = 0;
			var negative = 0;

			for ( var i = 0; i < Points.Length; i++ )
			{
				var v1 = Points[i];
				var v2 = Points[i < Points.Length - 1 ? i + 1 : 0];

				var cross = (point.x - v1.x) * (v2.y - v1.y) - (point.y - v1.y) * (v2.x - v1.x);

				switch ( cross )
				{
					case > 0:
						positive++;
						break;
					case < 0:
						negative++;
						break;
				}

				if ( positive > 0 && negative > 0 )
				{
					return false;
				}
			}

			return true;
		}

		private float CalculateArea()
		{
			var area = 0.0f;

			if ( Points.Length >= 3 )
			{
				var v1 = Points[0];

				for ( var i = 1; i < Points.Length - 1; i++ )
				{
					var v2 = Points[i];
					var v3 = Points[i + 1];
					var x1 = v2.x - v1.x;
					var y1 = v2.y - v1.y;
					var x2 = v3.x - v1.x;
					var y2 = v3.y - v1.y;

					area += MathF.Abs( x1 * y2 - x2 * y1 );
				}

				area = MathF.Abs( area * 0.5f );
			}

			return area;
		}
	}
}
