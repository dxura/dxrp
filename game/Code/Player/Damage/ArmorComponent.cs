namespace Dxura.RP.Game;

/// <summary>
///     A pawn might have armor, which reduces damage.
/// </summary>
public class ArmorComponent : Component, IDamageEvents
{
	[Property]
	[ReadOnly]
	[Sync( SyncFlags.FromHost )]
	public float Armor { get; set; }

	public float MaxArmor => Config.Current.Game.MaxArmor;

	[Property]
	[ReadOnly]
	[Sync( SyncFlags.FromHost )]
	public bool HasHelmet { get; set; }

	public void OnModifyDamageTaken( Component victim, ref DamageInfo damage )
	{
		if ( Armor > 0f )
		{
			DamageExtensions.AddFlag( ref damage, DamageFlags.Armor );
		}

		if ( HasHelmet )
		{
			DamageExtensions.AddFlag( ref damage, DamageFlags.Helmet );
		}
	}
}
