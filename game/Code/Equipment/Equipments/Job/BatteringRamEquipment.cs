namespace Dxura.RP.Game.Equipments;

public class BatteringRamEquipment : MeleeWeaponComponent
{
	[Property] [Group( "Effects" )] private SoundEvent? ImpactSound { get; set; }

	protected override void OnSwingImpact( GameObject hitObject, Surface surface, Vector3 pos, Vector3 normal )
	{
		if ( ImpactSound.IsValid() )
		{
			ImpactSound.Broadcast( pos );
		}
	}

	protected override bool CanSwing()
	{
		var canSwing = base.CanSwing();
		if ( !canSwing )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "battering:ram:swing:check", FireRate ) )
		{
			return false;
		}

		var trace = GetTrace();

		if ( trace is not { Hit: true } || !trace.Value.GameObject.IsValid() )
		{
			return false;
		}

		var hitObject = trace.Value.GameObject.Root;
		var owned = hitObject.GetComponent<IOwned>();
		var breachable = hitObject.GetComponent<IBreachable>();

		if ( owned == null || breachable == null )
		{
			return false;
		}

		var targetOwner = GameUtils.GetPlayerById( owned.Owner );

		if ( !targetOwner.IsValid() || !targetOwner.HasStatus( Constants.WarrantStatus ) )
		{
			Notify.Error( "#notify.warrant.batteringram.nowarrant" );
			return false;
		}

		BreachSystem.Instance.ChanceBreachHost( breachable, trace.Value.EndPosition );

		return true;
	}


}
