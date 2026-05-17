using Sandbox.Audio;

namespace Dxura.RP.Game.Entities;

[Title( "Radio" )]
[Category( "Entities" )]
public sealed class RadioEntity : BaseEntity, IOcclusionEvents
{
	private const float BasePlaybackVolume = 0.6f;
	private const float StationLoadDebounceSeconds = 1.5f;
	public const float StationChangeCooldown = 0.5f;

	[Property]
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnStationIndexChanged ) )]
	public int StationIndex { get; set; }

	[Property]
	[Change( nameof( OnIsPlayingChanged ) )]
	public bool IsPlaying { get; set; }

	[ConVar( "dx_radio_distance", ConVarFlags.Saved, Min = 0, Max = 5000 )]
	public static float RadioDistance { get; set; } = 600f;

	[ConVar( "dx_radio_autoplay", ConVarFlags.Saved )]
	public static bool AutoPlayRadios { get; set; } = false;

	[Property] [Group( "Audio" )] public Curve DropoffCurve { get; set; } = Curve.Linear.Reverse();

	[Property] [Group( "Effects" )] public GameObject? Model { get; set; }
	[Property] [Group( "Effects" )] public GameObject? Ui { get; set; }
	[Property] [Group( "Effects" )] public float PopAmplitude { get; set; } = 0.05f;
	[Property] [Group( "Effects" )] public float PopThreshold { get; set; } = 0.3f;
	[Property] [Group( "Effects" )] public float PopSmoothing { get; set; } = 10f;

	private MusicPlayer? _musicPlayer;
	private string? _currentStreamTitle;
	private float? _lastPlaybackTime;
	private TimeUntil _debouncedPlaybackStart;
	private bool _hasDebouncedPlaybackStart;
	private bool _hasInitializedLocalPlaybackState;
	private bool _occluded;
	private float _currentPop;
	
	public bool CanControl => Player.Local.IsValid() && GameUtils.HasPermission( Player.Local.SteamId, GameObject, false );
	public bool ShouldShowWorldPanel => !_occluded && IsLocalPlayerWithinDistance();

	public override string? DisplayName => Resource != null ? Resource.Name() : "Radio";

	public string CurrentStationName => GetStation( StationIndex ).Name;
	public string? CurrentStreamTitle => IsPlaying ? _currentStreamTitle : null;

	public static (string Name, string Url) GetStation( int index )
	{
		var stations = Config.Current.Game.RadioStations;
		if ( stations.Length == 0 )
		{
			return (string.Empty, string.Empty);
		}

		var i = ((index % stations.Length) + stations.Length) % stations.Length;
		var entry = stations[i];
		var sep = entry.IndexOf( '|' );

		if ( sep >= 0 )
		{
			return (entry[..sep], entry[(sep + 1)..]);
		}

		return (entry, entry);
	}

	private static int StationCount => Math.Max( 1, Config.Current.Game.RadioStations.Length );

	private void InitializePlaybackState()
	{
		if ( GameManager.IsHeadless || _hasInitializedLocalPlaybackState )
		{
			return;
		}

		if ( !AutoPlayRadios && !Player.Local.IsValid() )
		{
			return;
		}

		IsPlaying = AutoPlayRadios || Owner != 0 && Player.Local.IsValid() && Player.Local.SteamId == Owner;
		_hasInitializedLocalPlaybackState = true;
	}

	protected override void OnStart()
	{
		base.OnStart();

		InitializePlaybackState();
		
		if ( ShouldHavePlayback() )
		{
			StartPlayback();
		}

		SyncUiVisibility();
	}
	

	protected override void OnUpdate()
	{
		base.OnUpdate();
		InitializePlaybackState();

		SyncUiVisibility();

		if ( GameManager.IsHeadless || _musicPlayer == null )
		{
			if ( !GameManager.IsHeadless && _musicPlayer == null && ShouldHavePlayback() )
			{
				if ( _hasDebouncedPlaybackStart )
				{
					if ( _debouncedPlaybackStart )
					{
						_hasDebouncedPlaybackStart = false;
						StartPlayback();
					}
				}
				else
				{
					StartPlayback();
				}
			}

			return;
		}

		if ( !ShouldHavePlayback() )
		{
			StopPlayback();
			return;
		}

		ApplyPlaybackSettings();
		UpdateCurrentStreamTitle();

		// Pop scale to the beat
		if ( Model.IsValid() )
		{
			var amplitude = _musicPlayer.Amplitude;
			var target = amplitude > PopThreshold ? ( amplitude - PopThreshold ) * PopAmplitude : 0f;
			_currentPop = _currentPop.LerpTo( target, Time.Delta * PopSmoothing );
			Model.LocalScale = Vector3.One * ( 1f + _currentPop );
		}
	}

	public override void OnOcclusionChanged( bool occlude )
	{
		base.OnOcclusionChanged( occlude );

		_occluded = occlude;

		if ( GameManager.IsHeadless )
		{
			return;
		}

		if ( _occluded )
		{
			StopPlayback();
			SyncUiVisibility();
			return;
		}

		if ( ShouldHavePlayback() )
		{
			StartPlayback();
		}

		SyncUiVisibility();
	}

	private void StartPlayback()
	{
		StopPlayback();

		var (_, url) = GetStation( StationIndex );
		if ( string.IsNullOrWhiteSpace( url ) )
		{
			return;
		}

		_musicPlayer = MusicPlayer.PlayUrl( url );
		_currentStreamTitle = null;
		_lastPlaybackTime = null;
		_musicPlayer.TargetMixer = Mixer.FindMixerByName( "Radio" ) ?? Mixer.Master;
		ApplyPlaybackSettings();
	}

	private void ApplyPlaybackSettings()
	{
		if ( _musicPlayer == null )
		{
			return;
		}

		var radioDistance = Math.Max( 0f, RadioDistance );

		_musicPlayer.Position = WorldPosition;
		_musicPlayer.Distance = radioDistance;
		_musicPlayer.Falloff = DropoffCurve;
		_musicPlayer.Volume = BasePlaybackVolume * DxSound.RadioVolume.Clamp( 0f, 1f );
	}

	private void StopPlayback()
	{
		_musicPlayer?.Stop();
		_musicPlayer?.Dispose();
		_musicPlayer = null;
		_currentStreamTitle = null;
		_lastPlaybackTime = null;
		_debouncedPlaybackStart = 0f;
		_hasDebouncedPlaybackStart = false;
		_currentPop = 0f;
		
		if ( Model.IsValid() )
		{
			Model.LocalScale = Vector3.One;
		}
	}

	private void DebouncePlaybackStart()
	{
		_debouncedPlaybackStart = StationLoadDebounceSeconds;
		_hasDebouncedPlaybackStart = true;
	}

	private void UpdateCurrentStreamTitle()
	{
		if ( _musicPlayer == null || !IsPlaying )
		{
			return;
		}

		var playbackTime = _musicPlayer.PlaybackTime;
		var isActivelyPlaying = playbackTime > 0f &&
		                        (!_lastPlaybackTime.HasValue || !playbackTime.AlmostEqual( _lastPlaybackTime.Value ));

		_lastPlaybackTime = playbackTime;

		if ( !isActivelyPlaying )
		{
			return;
		}

		var title = _musicPlayer.Title;
		_currentStreamTitle = string.IsNullOrWhiteSpace( title ) ? null : title;
	}
	
	private void OnStationIndexChanged( int oldValue, int newValue )
	{
		StopPlayback();

		if ( ShouldHavePlayback() )
		{
			DebouncePlaybackStart();
		}
	}

	private void OnIsPlayingChanged( bool oldValue, bool newValue )
	{
		if ( GameManager.IsHeadless )
		{
			return;
		}

		if ( newValue && ShouldHavePlayback() )
		{
			_hasDebouncedPlaybackStart = false;
			StartPlayback();
		}
		else
		{
			_hasDebouncedPlaybackStart = false;
			StopPlayback();
		}
	}
	
	public void TogglePlayLocal()
	{
		IsPlaying = !IsPlaying;
	}

	private void SyncUiVisibility()
	{
		if ( Ui.IsValid() )
		{
			Ui.Enabled = ShouldShowWorldPanel;
		}
	}

	private bool ShouldHavePlayback()
	{
		return !GameManager.IsHeadless &&
		       IsPlaying &&
		       !_occluded &&
		       IsLocalPlayerWithinDistance() &&
		       !string.IsNullOrWhiteSpace( GetStation( StationIndex ).Url );
	}

	private bool IsLocalPlayerWithinDistance()
	{
		if ( !Player.Local.IsValid() )
		{
			return false;
		}

		var radioDistance = Math.Max( 0f, RadioDistance );
		return Player.Local.WorldPosition.DistanceSquared( WorldPosition ) <= radioDistance * radioDistance;
	}

	[Rpc.Host]
	public void NextStationHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:radio:station:{GameObject.Id}", StationChangeCooldown ) )
		{
			return;
		}

		if ( !GameUtils.HasPermission( Rpc.Caller, GameObject ) )
		{
			return;
		}

		if ( StationCount <= 1 )
		{
			var player = GameUtils.GetPlayerByConnectionId( callerId );
			player?.Error( "#entity.radio.no_stations" );
			return;
		}

		StationIndex = ( StationIndex + 1 ) % StationCount;
	}

	[Rpc.Host]
	public void PreviousStationHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:radio:station:{GameObject.Id}", StationChangeCooldown ) )
		{
			return;
		}

		if ( !GameUtils.HasPermission( Rpc.Caller, GameObject ) )
		{
			return;
		}

		if ( StationCount <= 1 )
		{
			var player = GameUtils.GetPlayerByConnectionId( callerId );
			player?.Error( "#entity.radio.no_stations" );
			return;
		}

		StationIndex = ( StationIndex - 1 + StationCount ) % StationCount;
	}

	protected override void OnDestroy()
	{
		StopPlayback();
		if ( Ui.IsValid() )
		{
			Ui.Enabled = false;
		}
		base.OnDestroy();
	}
}
