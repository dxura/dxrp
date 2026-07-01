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


	// ── Generic outline source discovery ────────────────────────────────────────────────────────
	// Any system (party, job team, admin ESP, …) implements IPlayerOutlineSource; TypeLibrary
	// discovers all concrete types automatically. The first source that returns a non-null request
	// wins; sources are queried in discovery order.

	private static List<IPlayerOutlineSource>? _outlineSources;

	private static List<IPlayerOutlineSource> OutlineSources
	{
		get
		{
			if ( _outlineSources is not null )
			{
				return _outlineSources;
			}

			_outlineSources = new List<IPlayerOutlineSource>();
			foreach ( var type in TypeLibrary.GetTypes<IPlayerOutlineSource>()
				         .Where( t => !t.IsAbstract && t.TargetType != null ) )
			{
				if ( TypeLibrary.Create<IPlayerOutlineSource>( type.TargetType ) is { } source )
				{
					_outlineSources.Add( source );
				}
			}

			return _outlineSources;
		}
	}

	private PlayerOutlineRequest? GetSourceOutlineRequest()
	{
		var localPlayer = Local;
		if ( !localPlayer.IsValid() || localPlayer == this || localPlayer.IsDead )
		{
			return null;
		}

		foreach ( var source in OutlineSources )
		{
			var request = source.GetOutlineRequest( localPlayer, this );
			if ( request.HasValue )
			{
				return request;
			}
		}

		return null;
	}

	private bool _sourceOutlineActive;
	private PlayerOutlineRequest _lastRequest;
	private List<Renderer>? _cachedOutlineTargets;

	private void OnUpdateEffects()
	{
		var request = GetSourceOutlineRequest();
		if ( request.HasValue )
		{
			var r = request.Value;
			if ( !_sourceOutlineActive )
			{
				_sourceOutlineActive = true;
				Outline.Enabled = true;
				Outline.OverrideTargets = r.OverrideTargets;
				if ( r.OverrideTargets )
				{
					_cachedOutlineTargets = GetOutlineTargets();
					Outline.Targets = _cachedOutlineTargets;
				}
			}

			if ( !r.Equals( _lastRequest ) )
			{
				_lastRequest = r;
				Outline.Width = r.Width;
				Outline.Color = r.Color;
				Outline.ObscuredColor = r.ObscuredColor;
				Outline.InsideColor = r.InsideColor;
				Outline.InsideObscuredColor = r.InsideObscuredColor;
			}

			return;
		}

		if ( _sourceOutlineActive )
		{
			_sourceOutlineActive = false;
			_cachedOutlineTargets = null;
			Outline.Enabled = false;
			Outline.OverrideTargets = false;
			Outline.Targets = null;
		}
	}

	/// <summary>
	/// Outline the dressed citizen body only — never equipment under hold bones, emotes, or nameplates.
	/// Targets must be <see cref="Renderer"/> instances, not <see cref="GameObject"/>.
	/// </summary>
	private List<Renderer> GetOutlineTargets()
	{
		var targets = new List<Renderer>();

		if ( Renderer.IsValid() && Renderer.Enabled && Renderer.Model is { IsError: false } )
		{
			targets.Add( Renderer );
		}

		if ( !BodyRoot.IsValid() )
		{
			return targets;
		}

		foreach ( var skinnedRenderer in BodyRoot.GetComponentsInChildren<SkinnedModelRenderer>( true ) )
		{
			if ( skinnedRenderer == Renderer || skinnedRenderer == EmoteRenderer )
			{
				continue;
			}

			// Dresser clothing only — equipment rigs live under hold_* and must not be outlined.
			if ( !skinnedRenderer.GameObject.Name.StartsWith( "Clothing - ", StringComparison.Ordinal ) )
			{
				continue;
			}

			if ( !skinnedRenderer.Enabled || skinnedRenderer.Model is null || skinnedRenderer.Model.IsError )
			{
				continue;
			}

			targets.Add( skinnedRenderer );
		}

		return targets;
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
