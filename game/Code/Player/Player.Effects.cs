namespace Dxura.RP.Game;

public partial class Player
{
	/// <summary>
	///     What effect should we spawn when a player gets headshot?
	/// </summary>
	[Property]
	[Feature( "Effects" )]
	private GameObject? HeadshotEffect { get; set; }

	/// <summary>
	///     What effect should we spawn when a player gets headshot while wearing a helmet?
	/// </summary>
	[Property]
	[Feature( "Effects" )]
	private GameObject? HeadshotWithHelmetEffect { get; set; }

	/// <summary>
	///     What effect should we spawn when we hit a player?
	/// </summary>
	[Property]
	[Feature( "Effects" )]
	private GameObject? BloodEffect { get; set; }

	/// <summary>
	///     What sound should we play when a player gets headshot?
	/// </summary>
	[Property]
	[Feature( "Effects" )]
	[Group( "Sounds" )]
	private SoundEvent? HeadshotSound { get; set; }

	/// <summary>
	///     What sound should we play when a player gets headshot?
	/// </summary>
	[Property]
	[Feature( "Effects" )]
	[Group( "Sounds" )]
	private SoundEvent HeadshotWithHelmetSound { get; } = null!;

	/// <summary>
	///     What sound should we play when we hit a player?
	/// </summary>
	[Property]
	[Feature( "Effects" )]
	[Group( "Sounds" )]
	private SoundEvent? BloodImpactSound { get; set; }

	/// <summary>
	///     What sound should we play when we change jobs?
	/// </summary>
	[Property]
	[Feature( "Effects" )]
	[Group( "Sounds" )]
	private SoundEvent? JobChangedSound { get; set; }

	[Property]
	[Feature( "Effects" )]
	[Group( "Sounds" )]
	public SoundEvent? LandSound { get; set; }

	/// <summary>
	///     The outline effect for this player.
	/// </summary>
	[RequireComponent]
	public HighlightOutline Outline { get; set; } = null!;


	private bool IsOutlineVisible()
	{
		var localPlayer = Local;
		if ( !localPlayer.IsValid() ||
		     localPlayer.HealthComponent.State != LifeState.Dead )
		{
			return false;
		}

		return localPlayer.GetLastKiller() == this;
	}

	private void OnUpdateEffects()
	{
		if ( !IsOutlineVisible() )
		{
			Outline.Enabled = false;
			return;
		}

		Outline.Enabled = true;
		Outline.Width = 0.1f;
		Outline.Color = Color.Transparent;
		Outline.InsideColor = HealthComponent.IsGodMode ? Color.White.WithAlpha( 0.1f ) : Color.Transparent;
		Outline.ObscuredColor = Color.Red;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void HandleHeadshotEffects( DamageInfo damageInfo, Vector3 position, Player? attacker, Player? victim )
	{
		// Non-local viewer
		if ( IsProxy )
		{
			var go = damageInfo.HasHelmet
				? HeadshotWithHelmetEffect?.Clone( position )
				: HeadshotEffect?.Clone( position );
		}

		var headshotSound = damageInfo.HasHelmet ? HeadshotWithHelmetSound : HeadshotSound;
		headshotSound.Play( position );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void HandleBodyshotEffects( Vector3 position )
	{
		if ( BloodEffect.IsValid() )
		{
			BloodEffect?.Clone( new CloneConfig
			{
				StartEnabled = true, Transform = new Transform( position ), Name = $"Blood effect from ({GameObject})"
			} );
		}

		if ( BloodImpactSound is not null )
		{
			BloodImpactSound.Play( position );
		}
	}
}
