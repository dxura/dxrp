namespace Dxura.RP.Game;

/// <summary>
///     A component that explodes and destroys its GameObject.
/// </summary>
public sealed class TimedExplodeComponent : Component
{
	/// <summary>
	///     How long until we explode.
	/// </summary>
	[Property]
	public float Time { get; set; } = 1f;

	/// <summary>
	///     The real time until we destroy the GameObject.
	/// </summary>
	[Property]
	[ReadOnly]
	private TimeUntil TimeUntilDestroy { get; set; } = 0;

	[Property]
	private float Damage { get; set; } = 100;

	[Property]
	public GameObject? Explosion { get; set; }


	protected override void OnStart()
	{
		TimeUntilDestroy = Time;
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( TimeUntilDestroy )
		{
			if ( Explosion.IsValid() )
			{
				var explosion = Explosion.Clone( WorldPosition );
				explosion.NetworkSpawn();

				var areaDamage = explosion.AddComponent<AreaDamage>();
				areaDamage.Damage = Damage;
				areaDamage.Attacker = GameUtils.GetPlayerByConnectionId( Network.OwnerId ) ?? (Component)this;
				areaDamage.Inflictor = this;
				areaDamage.TimeLimit = 0.1f;
				areaDamage.DamageFlags = DamageFlags.Explosion;
			}

			GameObject.Destroy();
		}
	}
	public long Owner { get; set; }
}
