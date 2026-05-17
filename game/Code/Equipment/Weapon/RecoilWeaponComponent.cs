namespace Dxura.RP.Game;

[Title( "Recoil" )]
[Group( "Weapon Components" )]
public class RecoilWeaponComponent : WeaponComponent
{
	[Property] [Category( "Recoil" )] public float ResetTime { get; set; } = 0.3f;

	// Recoil Patterns
	[Property]
	[ToggleGroup( "UseRecoilPattern" )]
	public bool UseRecoilPattern { get; set; } = false;

	[Property]
	[Category( "UseRecoilPattern" )]
	[HideIf( "UseRecoilPattern", false )]
	public Vector2 Scale { get; set; } = new( 2f, 5f );

	[Property]
	[Category( "UseRecoilPattern" )]
	[HideIf( "UseRecoilPattern", false )]
	public RecoilPattern RecoilPattern { get; set; } = new();

	[Property]
	[Group( "Standard Recoil" )]
	[HideIf( "UseRecoilPattern", true )]
	public RangedFloat HorizontalSpread { get; set; }

	[Property]
	[Group( "Standard Recoil" )]
	[HideIf( "UseRecoilPattern", true )]
	public RangedFloat VerticalSpread { get; set; }

	internal Angles Current { get; private set; }
	private int _currentFrame;

	private TimeSince _timeSinceLastShot;

	internal void Shoot()
	{
		if ( _timeSinceLastShot > ResetTime )
		{
			_currentFrame = 0;
		}

		_timeSinceLastShot = 0;

		var timeDelta = Time.Delta;

		if ( UseRecoilPattern )
		{
			var point = RecoilPattern.GetPoint( ref _currentFrame );

			var newAngles = new Angles( -point.y * Scale.y, -point.x * Scale.x, 0 ) * timeDelta;
			Current = Current + newAngles;
			_currentFrame++;
		}
		else
		{
			var newAngles = new Angles( -VerticalSpread.GetValue() * timeDelta, HorizontalSpread.GetValue() * timeDelta,
				0 );
			Current = Current + newAngles;
		}
	}

	protected override void OnUpdate()
	{
		if ( !Player.IsValid() )
		{
			return;
		}

		if ( Player.IsProxy )
		{
			return;
		}

		Current = Current.LerpTo( Angles.Zero, Time.Delta * 10f );
	}
}
