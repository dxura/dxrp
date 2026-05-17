namespace Dxura.RP.Game.Wire;

[Title( "Target" )]
[Category( "Wire" )]
[Icon( "cable" )]
public class TargetWire() : BaseWireConstruct( ConstructType.TargetWire ), IWireEvents, IDamageEvents
{
	private TargetWireData _data = new();

	public override string Name => "Target";

	[WireInput( "reset_damage" )]
	public bool ResetDamage
	{
		set
		{
			if ( value )
			{
				TotalDamage = 0;
			}
		}
		get => false; // This is just a trigger, no need to store state
	}

	[WireOutput( "total_damage" )]
	public float TotalDamage { get; private set; }

	[WireOutput( "damage" )]
	public float Damage { get; private set; }

	[WireOutput( "attacker" )]
	public string Attacker { get; private set; } = string.Empty;

	[WireOutput( "inflictor" )]
	public string Inflictor { get; private set; } = string.Empty;

	[WireOutput( "attacker_distance" )]
	public float AttackerDistance { get; private set; }

	[WireOutput( "attacker_accuracy" )]
	public int Accuracy { get; private set; }

	[WireOutput( "hits" )]
	public int Hits { get; private set; }

	[WireInput( "reset_hits" )]
	public bool ResetHits
	{
		set
		{
			if ( value )
			{
				Hits = 0;
			}
		}
		get => false; // This is just a trigger, no need to store state
	}

	public void OnDamageTakenHost( Component victim, DamageInfo damage )
	{
		Damage = damage.Damage;
		AttackerDistance = damage.Attacker.WorldPosition.Distance( WorldPosition );

		if ( damage.Attacker is Player player )
		{
			Attacker = player.SteamId.ToString();
		}
		else
		{
			Attacker = damage.Attacker.GameObject.Name;
		}

		TotalDamage = Math.Clamp( TotalDamage + damage.Damage, float.MinValue, float.MaxValue );

		const float radius = 18f;
		var hitDistance = damage.Position.Distance( WorldPosition );
		var normalizedDistance = Math.Min( hitDistance / radius, 1f ); // 0 = center, 1 = edge or beyond
		Accuracy = (int)Math.Clamp( (1f - normalizedDistance) * 100f, 0, 100 );

		Hits++;

		Inflictor = damage.Inflictor.IsValid() ? damage.Inflictor.GameObject.Name : "Unknown";
	}
}
