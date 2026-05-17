using Sandbox.Utility;

namespace Dxura.RP.Game;

public abstract class ScreenShake
{
	public abstract bool Update( CameraComponent camera );

	public class Random : ScreenShake
	{
		public float Progress => Easing.EaseOut( ((float)LifeTime).LerpInverse( 0, Length ) );

		private float Length { get; }
		private float Size { get; }
		private TimeSince LifeTime { get; } = 0f;

		public Random( float length = 1.5f, float size = 1f )
		{
			Length = length;
			Size = size;
		}

		public override bool Update( CameraComponent camera )
		{
			var random = Vector3.Random;
			random.z = 0f;
			random = random.Normal;

			camera.LocalPosition +=
				(camera.LocalRotation.Right * random.x + camera.LocalRotation.Up * random.y) *
				(1f - Progress) * Size;

			return LifeTime < Length;
		}
	}

	public class Fov : ScreenShake
	{
		public float Progress => ((float)LifeTime).LerpInverse( 0, Length );

		private float Length { get; }
		private float Amount { get; }
		private TimeSince LifeTime { get; } = 0f;
		private Curve Curve { get; }

		public Fov( float length = 1.5f, float size = 1f, Curve curve = default )
		{
			Length = length;
			Amount = size;
			Curve = curve;
		}

		public override bool Update( CameraComponent camera )
		{
			var c = Curve.Evaluate( Progress );

			Player.Local?.AddFieldOfViewOffset( Amount * c );

			return LifeTime < Length;
		}
	}

	public class Punch : ScreenShake
	{
		public float Progress => ((float)LifeTime).LerpInverse( 0, Length );

		private Vector3 Size { get; }
		private Angles PunchAngles { get; }
		private float Length { get; }
		private TimeSince LifeTime { get; } = 0f;
		private Curve Curve { get; }

		public Punch( float length = 1.5f, Vector3 size = default, Angles punch = default, Curve curve = default )
		{
			Length = length;
			Size = size;
			PunchAngles = punch;
			Curve = curve;
		}

		public override bool Update( CameraComponent camera )
		{
			var random = Size;

			var c = Curve.Evaluate( Progress );

			camera.LocalPosition += (camera.LocalRotation.Right * random.x +
			                         camera.LocalRotation.Up * random.y +
			                         camera.LocalRotation.Backward * random.z) * c * Size;
			camera.LocalRotation *= (PunchAngles * c).ToRotation();

			return LifeTime < Length;
		}
	}
}
