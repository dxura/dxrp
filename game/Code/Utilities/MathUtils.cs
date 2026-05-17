namespace Dxura.RP.Game.Utilities;

public static class MathUtils
{
	/// <summary>
	/// Calculates the rotation based on a surface normal and a direction to the player.
	/// Handles special case when the normal is close to vertical.
	/// </summary>
	/// <param name="normal">The surface normal vector.</param>
	/// <param name="toPlayer">The vector pointing towards the player.</param>
	/// <param name="offset">Offset rotation</param>
	/// <returns>The calculated rotation.</returns>
	public static Rotation CalculateSurfaceFlatRotation( Vector3 normal, Vector3 toPlayer, Rotation offset = default )
	{
		Angles angles;

		if ( Math.Abs( normal.Dot( Vector3.Up ) ) > 0.7f )
		{
			var playerDir = toPlayer;
			playerDir.z = 0;
			playerDir = playerDir.Normal;

			angles = normal.Dot( Vector3.Up ) > 0
				? new Angles( 90, (float)Math.Atan2( playerDir.y, playerDir.x ) * 180 / MathF.PI, 180 )
				: new Angles( -90, (float)Math.Atan2( playerDir.y, playerDir.x ) * 180 / MathF.PI, 180 );
		}
		else
		{
			angles = (-normal).EulerAngles;
			angles.roll = 0;
		}

		return angles.ToRotation() * offset;
	}

	public static List<Vector3> GetCurvedPoints( Vector3 start, Vector3 initialDirection, Vector3 end,
		int numberOfPoints )
	{
		var points = new List<Vector3>();

		// Calculate the control points
		var control1 = start + initialDirection * 10;
		var control2 = end - (end - start).Normal * 10 * initialDirection.Length;

		var step = 1.0f / (numberOfPoints - 1);

		for ( var i = 0; i < numberOfPoints; i++ )
		{
			var t = i * step;
			var point = CalculateCubicBezierPoint( t, start, control1, control2, end );
			points.Add( point );
		}

		return points;
	}

	public static Vector3 CalculateCubicBezierPoint( float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3 )
	{
		var u = 1 - t;
		var tt = t * t;
		var uu = u * u;
		var ttt = tt * t;
		var uuu = uu * u;

		var p = uuu * p0;
		p += 3 * uu * t * p1;
		p += 3 * u * tt * p2;
		p += ttt * p3;

		return p;
	}
}
