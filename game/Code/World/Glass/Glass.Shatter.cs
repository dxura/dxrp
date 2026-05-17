using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public sealed partial class Glass
{
	private static readonly ShatterType[] ShatterTypes = new[]
	{
		new ShatterType( 5, 10, 0.2f, 0.5f, 1.0f, 0.95f, 1.0f, 8.0f, 0.98f, false, 0.0f, 4 ),
		new ShatterType( 8, 14, 0.22f, 0.5f, 3.0f, 0.95f, 1.0f, 16.0f, 0.98f, false, 0.0f, 4 ),
		new ShatterType( 8, 10, 0.4f, 0.6f, 0.0f, 0.95f, 0.95f, 1.2f, 0.98f, false, 0.0f, 2 ),
		new ShatterType( 20, 20, 0.7f, 0.99f, 3.0f, 0.95f, 1.0f, 16.0f, 0.98f, false, 0.9f, 10 )
	};

	public void OnDamage( in Sandbox.DamageInfo damage )
	{
		// Convert world position to local space
		var localPosition = Transform.World.PointToLocal( damage.Position );
		var impactPoint = new Vector2( localPosition.x, localPosition.y );

		ShatterHost( impactPoint );
		DoShatter( impactPoint );
	}

	[Rpc.Host( NetFlags.Unreliable )]
	private void ShatterHost( Vector3 impactPoint )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:glass:shatter", Config.Current.Game.GlassShatterCooldown ) )
		{
			return;
		}

		using ( Rpc.FilterExclude( x => x.Id == callerId ) )
		{
			BroadcastShatter( impactPoint );
		}

		DoShatter( impactPoint );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void BroadcastShatter( Vector3 impactPoint )
	{
		if ( GameManager.IsHeadless )
		{
			return;
		}

		DoShatter( impactPoint );
	}

	private void DoShatter( Vector3 impactPoint )
	{
		// Find shard containing impact point
		foreach ( var shard in _shards.Values.Where( shard => shard.IsValid() )
			.Where( shard => shard.IsPointInside( impactPoint ) ) )
		{
			ShatterLocalSpace( shard, impactPoint, 0 );
			break;
		}
	}

	private void ShatterLocalSpace( Shard shard, Vector2 position, Vector3 impulse )
	{
		if ( !shard.IsValid() || !shard.IsPointInside( position ) )
		{
			return;
		}

		_lastShard = 0;

		var points = shard.Points.ToList();
		var transform = shard.PhysicsBody.Transform;
		var isLoose = shard.IsLoose;
		var impactClearRadius = Math.Clamp( MathF.Sqrt( CalculatePathArea( points ) ) * 0.45f, 8.0f, 48.0f );

		DestroyShard( shard );

		var shards = GenerateShatterShards( position, points, transform );
		foreach ( var newShard in shards )
		{
			if ( !newShard.IsValid() || newShard.Points.Length == 0 )
			{
				continue;
			}

			var shardCenter = CalculatePathCenter( newShard.Points );
			var isNearImpact = Vector2.DistanceBetween( shardCenter, position ) <= impactClearRadius;
			if ( isLoose || isNearImpact || !IsPathOnEdge( newShard.Points, Points, 0.1f ) )
			{
				var body = newShard.PhysicsBody;
				if ( body.IsValid() )
				{
					body.BodyType = PhysicsBodyType.Dynamic;
					body.ApplyImpulseAt( body.Transform.PointToWorld( position ), impulse * 0.1f );
				}

				newShard.IsLoose = true;
			}
		}
	}

	private List<Shard> GenerateShatterShards( Vector2 stressPosition, IList<Vector2> points, Transform transform )
	{
		var shards = new List<Shard>();
		var shatterType = ShatterTypes[1];

		var minX = points.Min( x => x.x );
		var minY = points.Min( x => x.y );
		var maxX = points.Max( x => x.x );
		var maxY = points.Max( x => x.y );

		var min = new Vector2( minX, minY );
		var max = new Vector2( maxX, maxY );

		var spokeLength = (max - min).LengthSquared;
		var numSpokes = Math.Max( 3, Sandbox.Game.Random.Int( shatterType.SpokesMin, shatterType.SpokesMax ) );
		var spokes = new List<ShatterSpoke>();

		var segmentRange = MathF.PI * 2.0f / numSpokes;
		var limitedRangeDeviation = Math.Min( segmentRange, MathF.PI * 2.0f * (1.0f / 3.0f) );

		for ( var i = 0; i < numSpokes; i++ )
		{
			var spokeRadians = i * segmentRange +
			                   Sandbox.Game.Random.Float( limitedRangeDeviation * -0.5f,
				                   limitedRangeDeviation * 0.5f ) * 0.9f;

			var spoke = new ShatterSpoke
			{
				OuterPos = new Vector2( stressPosition.x + spokeLength * MathF.Cos( spokeRadians ),
					stressPosition.y + spokeLength * MathF.Sin( spokeRadians ) ),
				IntersectionPos = Vector2.Zero,
				IntersectsEdgeIndex = -1,
				Length = -1
			};

			spokes.Insert( 0, spoke );
		}

		var edgeSegments = new List<ShatterEdgeSegment>();

		for ( var i = 0; i < points.Count; i++ )
		{
			var v1 = points[i];
			var v2 = points[i < points.Count - 1 ? i + 1 : 0];

			edgeSegments.Add( new ShatterEdgeSegment( v1, v2 ) );
		}

		for ( var spokeIndex = 0; spokeIndex < spokes.Count; spokeIndex++ )
		{
			for ( var edgeIndex = 0; edgeIndex < edgeSegments.Count; edgeIndex++ )
			{
				if ( !LineIntersect( edgeSegments[edgeIndex].Start, edgeSegments[edgeIndex].End,
					spokes[spokeIndex].OuterPos, stressPosition, out var point ) )
				{
					continue;
				}

				var spoke = spokes[spokeIndex];
				spoke.IntersectionPos = point;
				spoke.IntersectsEdgeIndex = edgeIndex;
				spoke.Length = Vector2.DistanceBetween( stressPosition, spoke.IntersectionPos );

				spokes[spokeIndex] = spoke;

				break;
			}
		}

		var centerHoleVertices = new List<Vector2>();

		for ( var spokeIndex = 0; spokeIndex < spokes.Count; spokeIndex++ )
		{
			var nextSpokeIndex = spokeIndex < spokes.Count - 1 ? spokeIndex + 1 : 0;
			var currentEdgeIndex = spokes[spokeIndex].IntersectsEdgeIndex;
			var nextEdgeIndex = spokes[nextSpokeIndex].IntersectsEdgeIndex;

			if ( nextSpokeIndex < 0 || currentEdgeIndex < 0 || nextEdgeIndex < 0 )
			{
				continue;
			}

			if ( spokes[spokeIndex].Length < 0.5f && spokes[nextSpokeIndex].Length < 0.5f )
			{
				continue;
			}

			var subShard = new List<Vector2>
			{
				stressPosition, spokes[spokeIndex].IntersectionPos
			};

			if ( currentEdgeIndex != nextEdgeIndex )
			{
				for ( var i = 0; i < 32 && currentEdgeIndex != nextEdgeIndex; i++ )
				{
					subShard.Add( edgeSegments[currentEdgeIndex].End );

					currentEdgeIndex = currentEdgeIndex < edgeSegments.Count - 1 ? currentEdgeIndex + 1 : 0;
				}
			}

			subShard.Add( spokes[nextSpokeIndex].IntersectionPos );

			Assert.True( subShard.Count >= 3 );

			var tipPoint1 = Vector2.Lerp( subShard[0], subShard[1],
				Sandbox.Game.Random.Float( shatterType.TipScaleMin, shatterType.TipScaleMax ) );
			var tipPoint2 = Vector2.Lerp( subShard[0], subShard[^1],
				Sandbox.Game.Random.Float( shatterType.TipScaleMin, shatterType.TipScaleMax ) );

			centerHoleVertices.Add( Vector2.Lerp( tipPoint1, tipPoint2, 0.5f ) );

			if ( shatterType.TipSpawnChance > 0 && Sandbox.Game.Random.Float( 0, shatterType.TipSpawnChance ) < 1.0f )
			{
				var tipShard = new List<Vector2>
				{
					subShard[0], tipPoint1, tipPoint2
				};
				ScaleVerts( tipShard, shatterType.TipScale );
				var shard = CreateShard( transform, tipShard );

				if ( shard != null )
				{
					shards.Add( shard );
				}
			}

			if ( shatterType.SecondTipSpawnChance > 0 &&
			     Sandbox.Game.Random.Float( 0, shatterType.SecondTipSpawnChance ) < 1.0f )
			{
				var secondTipPoint1 = Vector2.Lerp( tipPoint1, subShard[1], Sandbox.Game.Random.Float( 0.2f, 0.5f ) );
				var secondTopPoint2 = Vector2.Lerp( tipPoint2, subShard[^1], Sandbox.Game.Random.Float( 0.2f, 0.5f ) );

				var tipShard = new List<Vector2>
				{
					tipPoint1, secondTipPoint1, secondTopPoint2, tipPoint2
				};
				ScaleVerts( tipShard, shatterType.SecondShardScale );
				var shard = CreateShard( transform, tipShard );

				if ( shard != null )
				{
					shards.Add( shard );
				}

				tipPoint1 = secondTipPoint1;
				tipPoint2 = secondTopPoint2;
			}

			subShard.RemoveAt( 0 );
			subShard.Insert( 0, tipPoint1 );
			subShard.Add( tipPoint2 );

			if ( (tipPoint1 - tipPoint2).LengthSquared > 9.0f )
			{
				var vecBetweenCorners =
					Vector2.Lerp( Vector2.Lerp( tipPoint1, tipPoint2, Sandbox.Game.Random.Float( 0.4f, 0.6f ) ),
						stressPosition, Sandbox.Game.Random.Float( 0.1f, 0.3f ) );
				subShard.Add( vecBetweenCorners );
			}

			ScaleVerts( subShard, shatterType.ShardScale );
			var innerShard = CreateShard( transform, subShard );

			if ( innerShard != null )
			{
				shards.Add( innerShard );
			}
		}

		if ( !shatterType.HasCenterChunk || centerHoleVertices.Count <= 2 )
		{
			return shards;
		}

		var pShardCenter = new List<Vector2>();

		foreach ( var vertex in centerHoleVertices )
		{
			pShardCenter.Add( vertex );
		}

		ScaleVerts( pShardCenter, shatterType.CenterChunkScale );
		var newShard = CreateShard( transform, pShardCenter );

		if ( newShard != null )
		{
			shards.Add( newShard );
		}

		return shards;
	}

	private struct ShatterType(
		int spokesMin,
		int spokesMax,
		float tipScaleMin,
		float tipScaleMax,
		float tipSpawnChance,
		float tipScale,
		float shardScale,
		float secondTipSpawnChance,
		float secondShardScale,
		bool hasCenterChunk,
		float centerChunkScale,
		int shardLimit )
	{
		public readonly int SpokesMin = spokesMin;
		public readonly int SpokesMax = spokesMax;
		public readonly float TipScaleMin = tipScaleMin;
		public readonly float TipScaleMax = tipScaleMax;
		public readonly float TipSpawnChance = tipSpawnChance;
		public readonly float TipScale = tipScale;
		public readonly float ShardScale = shardScale;
		public readonly float SecondTipSpawnChance = secondTipSpawnChance;
		public readonly float SecondShardScale = secondShardScale;
		public readonly bool HasCenterChunk = hasCenterChunk;
		public readonly float CenterChunkScale = centerChunkScale;
		public int ShardLimit = shardLimit;
	}

	private struct ShatterSpoke
	{
		public Vector2 OuterPos;
		public Vector2 IntersectionPos;
		public int IntersectsEdgeIndex;
		public float Length;
	}

	private struct ShatterEdgeSegment( Vector2 start, Vector2 end )
	{
		public readonly Vector2 Start = start;
		public readonly Vector2 End = end;
	}
}
