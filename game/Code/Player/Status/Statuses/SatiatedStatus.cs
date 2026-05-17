namespace Dxura.RP.Game.Statuses;

public class SatiatedStatus : BaseStatus
{
	public override string Id => Constants.SatiatedStatus;
	public override string Name => "Satiated";

	public override string? MaterialIcon => "pregnant_woman";
	public override Color Color => Color.FromRgb( 0xFF9800 );

	public override float? DefaultDuration => 300f;

	public override bool Stackable => true;
	public override int MaxStacks => 3;

	public override bool RemoveOnRespawn => true;

	private TimeSince _timeSinceLastHeal = 0;

	private SoundEvent? HealingSound => ResourceLibrary.Get<SoundEvent>( "sounds/player/passive/burp.sound" );

	public override void OnSecondlyUpdateServer( Player player )
	{
		base.OnSecondlyUpdateServer( player );

		// Don't bother if player is dead or health is already full
		if ( !player.HealthComponent.IsValid() || player.IsDead )
		{
			return;
		}

		if ( player.HealthComponent.Health >= player.HealthComponent.MaxHealth )
		{
			return;
		}

		// Only heal once per second
		if ( _timeSinceLastHeal < 1f )
		{
			return;
		}

		_timeSinceLastHeal = 0;

		// Calculate healing per second based on stacks
		var healAmount = Config.Current.Game.SatiatedHealPerStack * CurrentStacks;

		// Apply healing but don't exceed max health
		player.HealthComponent.Health = Math.Min( player.HealthComponent.MaxHealth, player.HealthComponent.Health + healAmount );

		// Play healing sound effect
		if ( Random.Shared.Int( 0, 100 ) < 15 )
		{
			HealingSound.BroadcastHost( player.WorldPosition, player.GameObject );
		}
	}
}
