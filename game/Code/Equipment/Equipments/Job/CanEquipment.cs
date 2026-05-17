namespace Dxura.RP.Game.Equipments;

public class CanEquipment : InputWeaponComponent
{
	[Property] [Group( "Effects" )]
	public required SoundEvent JiggleSoundEvent { get; set; }
	[Property] [Group( "Effects" )]
	public required SoundEvent TauntSoundEvent { get; set; }

	protected override void OnInputDown()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "can", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var taunt = Input.Down( "Attack2" );

		if ( taunt && !Cooldown.Current.CheckAndStartCooldown( "hobo:taunt", Config.Current.Game.HoboTauntCooldown, true ) )
		{
			TauntSoundEvent.Broadcast( Player.Local.WorldPosition, Player.Local.GameObject );
		}
		else
		{
			JiggleSoundEvent.Broadcast( Player.Local.WorldPosition, Player.Local.GameObject );
		}
	}
}
