using Dxura.RP.Game.Utilities;

namespace Dxura.RP.Game.Equipments;

public sealed class TaserArcEffect : Component
{
	private static readonly Color ElectricColor = new( 0.35f, 0.85f, 1f, 1f );

	private LineRenderer? _line;
	private readonly List<Vector3> _points = new();
	private Vector3 _start;
	private Vector3 _end;
	private Vector3 _forward;
	private TimeUntil _timeUntilDestroy;

	public float Duration { get; set; } = 0.15f;
	public float Width { get; set; } = 0.35f;
	public float JitterStrength { get; set; } = 6f;

	public void Initialize( Vector3 start, Vector3 end, Vector3 forward )
	{
		_start = start;
		_end = end;
		_forward = forward.Normal;
		_timeUntilDestroy = Duration;

		_line = GameObject.Components.Create<LineRenderer>();
		ConfigureLine( _line );
		UpdateArc();
	}

	protected override void OnUpdate()
	{
		if ( _timeUntilDestroy )
		{
			GameObject.Destroy();
			return;
		}

		UpdateArc();
	}

	public static void Spawn( Scene scene, Vector3 start, Vector3 end, Vector3 forward, float duration = 0.15f, float width = 0.35f )
	{
		if ( GameManager.IsHeadless )
		{
			return;
		}

		if ( start.Distance( end ) < 1f )
		{
			end = start + forward.Normal * 32f;
		}

		var gameObject = scene.CreateObject();
		gameObject.Name = "Taser Arc";

		var effect = gameObject.Components.Create<TaserArcEffect>();
		effect.Duration = duration;
		effect.Width = width;
		effect.Initialize( start, end, forward );
	}

	private void ConfigureLine( LineRenderer line )
	{
		line.Enabled = true;
		line.Additive = true;
		line.AutoCalculateNormals = true;
		line.CastShadows = false;
		line.Color = ElectricColor;
		line.CylinderSegments = 4;
		line.DepthFeather = 0;
		line.Face = SceneLineObject.FaceMode.Camera;
		line.FogStrength = 0.35f;
		line.Lighting = false;
		line.Opaque = false;
		line.SplineInterpolation = 4;
		line.UseVectorPoints = true;
		line.Width = Width;
	}

	private void UpdateArc()
	{
		if ( !_line.IsValid() )
		{
			return;
		}

		var distance = _start.Distance( _end );
		var segmentCount = Math.Clamp( (int)(distance / 12f), 4, 24 );
		var curved = MathUtils.GetCurvedPoints( _start, _forward, _end, segmentCount );
		_points.Clear();

		for ( var i = 0; i < curved.Count; i++ )
		{
			var point = curved[i];
			if ( i > 0 && i < curved.Count - 1 )
			{
				var falloff = MathF.Sin( i / (float)(curved.Count - 1) * MathF.PI );
				point += Vector3.Random * JitterStrength * falloff;
			}

			_points.Add( point );
		}

		_line.VectorPoints = _points;
	}
}
