namespace Dxura.RP.Game;

public sealed partial class Glass
{
	private static bool LineIntersect( Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection )
	{
		intersection = Vector2.Zero;

		var xD1 = p2.x - p1.x;
		var xD2 = p4.x - p3.x;
		var yD1 = p2.y - p1.y;
		var yD2 = p4.y - p3.y;
		var xD3 = p1.x - p3.x;
		var yD3 = p1.y - p3.y;

		var length1 = MathF.Sqrt( xD1 * xD1 + yD1 * yD1 );
		var length2 = MathF.Sqrt( xD2 * xD2 + yD2 * yD2 );

		var dot = xD1 * xD2 + yD1 * yD2;
		var deg = dot / (length1 * length2);

		if ( Math.Abs( Math.Abs( deg ) - 1.0f ) < 0.1f )
		{
			return false;
		}

		var div = yD2 * xD1 - xD2 * yD1;
		var ua = (xD2 * yD3 - yD2 * xD3) / div;
		intersection.x = p1.x + ua * xD1;
		intersection.y = p1.y + ua * yD1;

		xD1 = intersection.x - p1.x;
		xD2 = intersection.x - p2.x;
		yD1 = intersection.y - p1.y;
		yD2 = intersection.y - p2.y;
		var segmentLength1 = MathF.Sqrt( xD1 * xD1 + yD1 * yD1 ) + MathF.Sqrt( xD2 * xD2 + yD2 * yD2 );

		xD1 = intersection.x - p3.x;
		xD2 = intersection.x - p4.x;
		yD1 = intersection.y - p3.y;
		yD2 = intersection.y - p4.y;
		var segmentLength2 = MathF.Sqrt( xD1 * xD1 + yD1 * yD1 ) + MathF.Sqrt( xD2 * xD2 + yD2 * yD2 );

		return !(MathF.Abs( length1 - segmentLength1 ) > 0.01f) && !(MathF.Abs( length2 - segmentLength2 ) > 0.01f);
	}

	private static bool IsPathClockwise( IList<Vector2> points )
	{
		float area = 0;
		for ( var i = 0; i < points.Count; i++ )
		{
			var j = (i + 1) % points.Count;
			area += (points[j].x - points[i].x) * (points[j].y + points[i].y);
		}

		return area < 0;
	}

	private static void ComputeTriangleNormalAndTangent( out Vector3 outNormal, out Vector4 outTangent, Vector3 v0,
		Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2 )
	{
		outNormal = ComputeTriangleNormal( v0, v1, v2 );
		ComputeTriangleTangentSpace( v0, v1, v2, uv0, uv1, uv2, out var faceS, out var faceT );
		outTangent = ComputeTangentForFace( faceS, faceT, outNormal );
	}

	private static Vector3 ComputeTriangleNormal( Vector3 v1, Vector3 v2, Vector3 v3 )
	{
		var e1 = v2 - v1;
		var e2 = v3 - v1;
		return (Vector3.Cross( e1, e2 ).Normal + Vector3.Random * 0.1f).Normal;
	}

	private static void ComputeTriangleTangentSpace( Vector3 p0, Vector3 p1, Vector3 p2, Vector2 t0, Vector2 t1,
		Vector2 t2, out Vector3 s, out Vector3 t )
	{
		const float epsilon = 1e-12f;

		s = Vector3.Zero;
		t = Vector3.Zero;

		var edge0 = new Vector3( p1.x - p0.x, t1.x - t0.x, t1.y - t0.y );
		var edge1 = new Vector3( p2.x - p0.x, t2.x - t0.x, t2.y - t0.y );

		var cross = Vector3.Cross( edge0, edge1 );

		if ( MathF.Abs( cross.x ) > epsilon )
		{
			s.x += -cross.y / cross.x;
			t.x += -cross.z / cross.x;
		}

		edge0 = new Vector3( p1.y - p0.y, t1.x - t0.x, t1.y - t0.y );
		edge1 = new Vector3( p2.y - p0.y, t2.x - t0.x, t2.y - t0.y );

		cross = Vector3.Cross( edge0, edge1 );

		if ( MathF.Abs( cross.x ) > epsilon )
		{
			s.y += -cross.y / cross.x;
			t.y += -cross.z / cross.x;
		}

		edge0 = new Vector3( p1.z - p0.z, t1.x - t0.x, t1.y - t0.y );
		edge1 = new Vector3( p2.z - p0.z, t2.x - t0.x, t2.y - t0.y );

		cross = Vector3.Cross( edge0, edge1 );

		if ( MathF.Abs( cross.x ) > epsilon )
		{
			s.z += -cross.y / cross.x;
			t.z += -cross.z / cross.x;
		}

		s = s.Normal;
		t = t.Normal;
	}

	private static Vector4 ComputeTangentForFace( Vector3 faceS, Vector3 faceT, Vector3 normal )
	{
		var leftHanded = Vector3.Dot( Vector3.Cross( faceS, faceT ), normal ) < 0.0f;
		var tangent = Vector4.Zero;

		if ( !leftHanded )
		{
			faceT = Vector3.Cross( normal, faceS );
			faceS = Vector3.Cross( faceT, normal );
			faceS = faceS.Normal;

			tangent.x = faceS[0];
			tangent.y = faceS[1];
			tangent.z = faceS[2];
			tangent.w = 1.0f;
		}
		else
		{
			faceT = Vector3.Cross( faceS, normal );
			faceS = Vector3.Cross( normal, faceT );
			faceS = faceS.Normal;

			tangent.x = faceS[0];
			tangent.y = faceS[1];
			tangent.z = faceS[2];
			tangent.w = -1.0f;
		}

		return tangent;
	}

	private static float DistanceToEdge( Vector2 point, Vector2 start, Vector2 end )
	{
		var delta = end - start;
		var length = delta.Length;
		var direction = delta / length;
		var closestPoint = start + Vector3.Dot( point - start, direction ).Clamp( 0, length ) * direction;
		return (point - closestPoint).Length;
	}

	private static bool IsPathOnEdge( IList<Vector2> path1, IList<Vector2> path2, float threshold )
	{
		foreach ( var point in path1 )
		{
			for ( var i = 0; i < path2.Count; i++ )
			{
				var dist = DistanceToEdge( point, path2[i], path2[(i + 1) % path2.Count] );
				if ( dist <= threshold )
				{
					return true;
				}
			}
		}

		return false;
	}

	private static float CalculatePathArea( IList<Vector2> points )
	{
		float area = 0;
		var vertexCount = points.Count;

		if ( vertexCount < 3 )
		{
			return 0;
		}

		var v1 = points[0];
		for ( var i = 1; i < vertexCount - 1; i++ )
		{
			var v2 = points[i];
			var v3 = points[i + 1];

			var x1 = v2.x - v1.x;
			var y1 = v2.y - v1.y;
			var x2 = v3.x - v1.x;
			var y2 = v3.y - v1.y;

			area += MathF.Abs( x1 * y2 - x2 * y1 );
		}

		return MathF.Abs( area * 0.5f );
	}

	private static Vector2 CalculatePathCenter( IList<Vector2> points )
	{
		if ( points.Count == 0 )
		{
			return Vector2.Zero;
		}

		var center = Vector2.Zero;
		foreach ( var point in points )
		{
			center += point;
		}

		return center / points.Count;
	}

	private static void ScaleVerts( List<Vector2> points, float scale )
	{
		if ( scale <= 0.0f )
		{
			return;
		}

		var average = Vector2.Zero;
		var pointCount = points.Count;

		if ( pointCount > 0 )
		{
			foreach ( var point in points )
			{
				average += point;
			}

			average /= pointCount;
		}

		for ( var i = 0; i < points.Count; ++i )
		{
			points[i] = Vector2.Lerp( average, points[i], scale );
		}
	}
}
