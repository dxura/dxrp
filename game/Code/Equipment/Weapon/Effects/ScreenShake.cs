namespace Dxura.RP.Game;

[Title( "On Shot - Screen Shake" )]
[Category( "Weapon Components" )]
[Icon( "pending" )]
public class ScreenShakeOnShot : WeaponComponent, IGameEvents
{
	[Property] public float Length { get; set; } = 0.3f;
	[Property] public float Size { get; set; } = 1.05f;

	public void OnWeaponShot()
	{
		var shake = new ScreenShake.Random( Length, Size );
		ScreenShaker.Main?.Add( shake );
	}
}
