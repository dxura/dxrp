namespace Dxura.RP.Game;

[Title( "On Shot - Bolt" )]
[Icon( "pending" )]
[Group( "Weapon Components" )]
public class BoltPusher : WeaponComponent, IGameEvents
{
	[Property]
	private float Delay { get; set; } = 0.3f;
	[Property]
	private float DelayOut { get; set; } = 0.5f;
	[Property]
	private SoundEvent? BoltSound { get; set; }

	private TimeUntil BoltDelay { get; set; } = 0f;

	private bool Bolting => _boltStage > 0;

	private int _boltStage;

	protected override void OnStart()
	{
		BindTag( "bolting", () => Bolting );
	}

	protected override void OnUpdate()
	{
		if ( BoltDelay && _boltStage == 2 )
		{
			if ( !Equipment.ViewModel.IsValid() || Equipment.ViewModel.Enabled == false )
			{
				_boltStage = 0;
				return;
			}

			_boltStage = 1;
			BoltDelay = DelayOut;

			Equipment.ViewModel.GameObject.PlaySound( BoltSound );
			if ( !Equipment.ViewModel.IsValid() || Equipment.ViewModel.Enabled == false )
			{
				return;
			}
			Equipment.ViewModel.ModelRenderer.Set( "b_reload_bolt", true );
		}
		else if ( BoltDelay && _boltStage == 1 )
		{
			_boltStage = 0;
		}
	}

	private bool CanBolt()
	{
		return Components.TryGet<AmmoComponent>( out var ammo ) && ammo.Ammo > 0;
	}

	public void OnWeaponShot()
	{
		if ( !CanBolt() )
		{
			return;
		}
		_boltStage = 2;
		BoltDelay = Delay;
	}
}
