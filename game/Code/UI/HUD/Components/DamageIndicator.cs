using System.Threading.Tasks;

public class DamageIndicator : Panel
{
	public static DamageIndicator Current = null!;

	public DamageIndicator()
	{
		Current = this;
	}

	public void OnHit( Vector3 pos )
	{
		var p = new HitPoint( pos );
		p.Parent = this;
	}

	public class HitPoint : Panel
	{
		public Vector3 Position;

		public HitPoint( Vector3 pos )
		{
			Position = pos;

			_ = Lifetime();
		}

		public override void Tick()
		{
			base.Tick();

			var wpos = Scene.Camera.WorldRotation.Inverse *
			           (Position.WithZ( 0 ) - Scene.Camera.Transform.World.Position.WithZ( 0 )).Normal;
			wpos = wpos.WithZ( 0 ).Normal;

			var angle = MathF.Atan2( wpos.y, -1.0f * wpos.x );

			var pt = new PanelTransform();

			pt.AddTranslateX( Length.Percent( -50.0f ) );
			pt.AddTranslateY( Length.Percent( -0.0f ) );
			pt.AddRotation( 0, 0, angle.RadianToDegree() );

			Style.Transform = pt;
		}

		private async Task Lifetime()
		{
			await GameTask.Delay( 1000 );
			AddClass( "dying" );
			await GameTask.Delay( 500 );
			Delete();
		}
	}
}
