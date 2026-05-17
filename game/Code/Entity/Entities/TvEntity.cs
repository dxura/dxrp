using Dxura.RP.Game.Tools;
using System.Threading.Tasks;
using WorldPanel=Sandbox.WorldPanel;

namespace Dxura.RP.Game.Entities;

public class TvEntity : BaseEntity, IDescription, Component.IPressable
{
	private const float CheckInterval = 1.5f;
	private const float WebUnmuteDelaySeconds = 2.5f;
	private const float WebReloadMinIntervalSeconds = 3.0f;
	private const float VolumeChangeDebounceSeconds = 0.6f;
	[Property] public required Web Web { get; set; }

	[Property] public string? OwnerJobIdentifier { get; set; }

	public GameModeJobDto? OwnerJob => string.IsNullOrWhiteSpace( OwnerJobIdentifier )
		? null
		: GameModeJobs.FindByReference( OwnerJobIdentifier );

	[Sync( SyncFlags.FromHost )] private TimeSince? TimeSinceVideoPlay { get; set; } = 0;

	[Property]
	[Sync( SyncFlags.FromHost )]
	private string? Video { get; set; }
	[Property]
	[Sync( SyncFlags.FromHost )]
	private string? Playlist { get; set; }
	[ConVar( "dx_tv_range", ConVarFlags.Saved, Min = 0, Max = 5000 )]
	private static int TvRange { get; set; } = 600;

	// True if the TV is currently active and playing content
	private bool _isCurrentlyPlaying;

	// True if the player has the TV turned on (can see/hear it)
	private bool _isTvTurnedOn = true;

	private TimeSince _lastCheck = 0;
	private int _lastAppliedEffectiveVolume = -1;
	private int _lastObservedEffectiveVolume = -1;
	private float _lastObservedTvMixer = -1f;
	private TimeSince _timeSinceVolumeChanged;
	private TimeSince _timeSinceLastWebReload;
	private long _webUrlNonce;

	public new string DisplayName { get; private set; } = string.Empty;

	protected override void OnStart()
	{
		base.OnStart();

		DisplayName = Language.GetPhrase( "entity.tv.name" );
		StopPlayback();

		// Set name based on owner (if available)
		var owner = GameUtils.GetPlayerById( Owner );
		if ( owner.IsValid() )
		{
			DisplayName = string.Format( Language.GetPhrase( "entity.tv.display_name" ), owner.DisplayName );
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		SyncLocalTvVolume();

		if ( !ShouldUpdatePlayback() )
		{
			return;
		}

		UpdatePlaybackState();
	}

	private bool ShouldUpdatePlayback()
	{
		return _lastCheck >= CheckInterval &&
		       Player.Local.IsValid() &&
		       _isTvTurnedOn;
	}

	private void UpdatePlaybackState()
	{
		_lastCheck = 0;
		var playerPosition = Player.Local.Controller.EyePosition;
		var playerDistance = playerPosition.Distance( WorldPosition );

		// Only do raycast check if we're within basic distance range
		if ( playerDistance <= TvRange )
		{
			// Perform raycast from player to TV to check for obstacles
			var originPos = WorldPosition + WorldRotation * (Vector3.Up * 30 * WorldScale.z) + WorldRotation * Vector3.Forward * 5;
			var direction = (originPos - playerPosition).Normal;

			var trace = Scene.Trace.Ray( new Ray( playerPosition, direction ), TvRange )
				.WithTag( "solid" )
				.WithoutTags( Constants.PlayerTag )
				.Run();

			var isVisible = trace.GameObject == GameObject;

			switch ( _isCurrentlyPlaying )
			{
				case false when playerDistance <= TvRange && isVisible && HasVideoContent():
					StartPlayback();
					break;
				case true when playerDistance > TvRange || !isVisible:
					StopPlayback();
					break;
			}
		}
		else if ( _isCurrentlyPlaying )
		{
			StopPlayback();
		}
	}

	private bool HasVideoContent()
	{
		return (!string.IsNullOrWhiteSpace( Video ) || !string.IsNullOrWhiteSpace( Playlist )) && TimeSinceVideoPlay.HasValue;
	}

	[Rpc.Host]
	public void PlayYoutubeHost( string id, string playlistId, int videoOffset )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:tv", Config.Current.Game.TvCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !HasTvPermission( player ) )
		{
			player.Error( "#generic.permission" );
			return;
		}

		var normalizedVideoId = string.IsNullOrWhiteSpace( id ) ? null : id.Trim();
		var normalizedPlaylistId = string.IsNullOrWhiteSpace( playlistId ) ? null : playlistId.Trim();

		if ( normalizedVideoId == null && normalizedPlaylistId == null )
		{
			player.Error( "#entity.tv.invalid_id" );
			return;
		}

		if ( normalizedVideoId != null && !IsValidYouTubeVideoId( normalizedVideoId ) )
		{
			player.Error( "#entity.tv.invalid_id" );
			return;
		}

		if ( normalizedPlaylistId != null && !IsValidYouTubePlaylistId( normalizedPlaylistId ) )
		{
			player.Error( "#entity.tv.invalid_id" );
			return;
		}

		if ( videoOffset < 0 || videoOffset > 10000 )
		{
			player.Error( "#entity.tv.invalid_offset" );
			return;
		}

		BroadcastStopPlayback();

		// Set new video properties
		TimeSinceVideoPlay = videoOffset;
		Video = normalizedVideoId;
		Playlist = normalizedPlaylistId;

		_ = ServerApiClient.Audit( "TV", $"{player.SteamName} ({player.SteamId}) {BuildYouTubeAuditUrl( normalizedVideoId, normalizedPlaylistId )}", player.SteamId );
	}

	[Rpc.Host]
	public void StopHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:tv", Config.Current.Game.TvCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !HasTvPermission( player ) )
		{
			player.Error( "#generic.permission" );
			return;
		}

		Video = null;
		Playlist = null;
		TimeSinceVideoPlay = null;
		StopPlayback();
	}


	private void StartPlayback()
	{
		if ( !_isTvTurnedOn )
		{
			return;
		}

		_isCurrentlyPlaying = true;
		Web.GameObject.Enabled = true;

		PlayVideo();
	}

	/// <summary>
	/// Server-side validation for YouTube video IDs to prevent injection attacks
	/// </summary>
	private static bool IsValidYouTubeVideoId( string videoId )
	{
		if ( string.IsNullOrWhiteSpace( videoId ) )
		{
			return false;
		}

		// YouTube video IDs are exactly 11 characters long
		if ( videoId.Length != 11 )
		{
			return false;
		}

		// Only allow alphanumeric characters, hyphens, and underscores
		foreach ( var c in videoId )
		{
			if ( !char.IsLetterOrDigit( c ) && c != '-' && c != '_' )
			{
				return false;
			}
		}

		return true;
	}

	private static bool IsValidYouTubePlaylistId( string playlistId )
	{
		if ( string.IsNullOrWhiteSpace( playlistId ) )
		{
			return false;
		}

		if ( playlistId.Length is < 10 or > 128 )
		{
			return false;
		}

		foreach ( var c in playlistId )
		{
			if ( !char.IsLetterOrDigit( c ) && c != '-' && c != '_' )
			{
				return false;
			}
		}

		return true;
	}

	private static string BuildYouTubeAuditUrl( string? videoId, string? playlistId )
	{
		var url = "https://www.youtube.com/";

		if ( !string.IsNullOrWhiteSpace( videoId ) )
		{
			url += $"watch?v={Uri.EscapeDataString( videoId )}";
		}
		else
		{
			url += "playlist";
		}

		if ( !string.IsNullOrWhiteSpace( playlistId ) )
		{
			url += $"{(url.Contains( '?' ) ? "&" : "?")}list={Uri.EscapeDataString( playlistId )}";
		}

		return url;
	}

	public bool HasTvPermission( Player player )
	{
		if ( !this.IsValid() )
		{
			return false;
		}

		// Check direct entity owner first
		if ( Owner == player.SteamId )
		{
			return true;
		}

		// Check job ownership second
		if ( !string.IsNullOrWhiteSpace( OwnerJobIdentifier ) &&
		     OwnerJob?.Id == player.Job.Id )
		{
			return true;
		}

		// Last resort check friendship, but only if there is an owner
		var ownerPlayer = GameUtils.GetPlayerById( Owner );
		if ( ownerPlayer.IsValid() && FriendSystem.Instance.HasConstructPermission( ownerPlayer.SteamId, player.SteamId ) )
		{
			return true;
		}

		return false;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastStopPlayback()
	{
		StopPlayback();
	}

	private void StopPlayback()
	{
		_isCurrentlyPlaying = false;
		_lastAppliedEffectiveVolume = -1;
		_lastObservedEffectiveVolume = -1;
		_lastObservedTvMixer = -1f;
		_webUrlNonce++;
		_timeSinceLastWebReload = 999f;
		if ( Web.IsValid() )
		{
			Web.GameObject.Enabled = false;
		}
	}

	private void PlayVideo()
	{
		if ( !HasVideoContent() )
		{
			return;
		}

		var tvMixer = DxSound.TvVolume.Clamp( 0f, 1f );
		var effectiveVolume = (int)Math.Round( 35 * tvMixer );
		_lastObservedTvMixer = tvMixer;
		_lastAppliedEffectiveVolume = effectiveVolume;
		_lastObservedEffectiveVolume = effectiveVolume;
		_timeSinceVolumeChanged = 0;
		_timeSinceLastWebReload = 999f;
		ReloadWebUrlForPlayback( effectiveVolume );
	}

	private void ReloadWebUrlForPlayback( int effectiveVolume )
	{
		if ( !Web.IsValid() || !HasVideoContent() )
		{
			return;
		}

		_webUrlNonce++;
		_timeSinceLastWebReload = 0;

		var start = (int)TimeSinceVideoPlay!.Value;
		var videoParam = string.IsNullOrWhiteSpace( Video ) ? string.Empty : $"videoId={Uri.EscapeDataString( Video )}&";
		var playlistParam = string.IsNullOrWhiteSpace( Playlist ) ? string.Empty : $"&playlistId={Uri.EscapeDataString( Playlist )}";
		Web.Url = $"https://dxrp.net/embed/youtube?{videoParam}volume={effectiveVolume}&start={start}{playlistParam}";

		_ = UnmuteAfterDelay( _webUrlNonce );
	}

	private void SyncLocalTvVolume()
	{
		if ( !_isCurrentlyPlaying || !HasVideoContent() || !Web.IsValid() )
		{
			return;
		}

		var tvMixer = DxSound.TvVolume.Clamp( 0f, 1f );
		var effectiveVolume = (int)Math.Round( 35 * tvMixer );

		const float mixerEpsilon = 0.0001f;
		if ( MathF.Abs( tvMixer - _lastObservedTvMixer ) > mixerEpsilon )
		{
			_lastObservedTvMixer = tvMixer;
			_lastObservedEffectiveVolume = effectiveVolume;
			_timeSinceVolumeChanged = 0;
		}
		else if ( effectiveVolume != _lastObservedEffectiveVolume )
		{
			_lastObservedEffectiveVolume = effectiveVolume;
			_timeSinceVolumeChanged = 0;
		}

		if ( _lastObservedEffectiveVolume < 0 || _lastObservedEffectiveVolume == _lastAppliedEffectiveVolume )
		{
			return;
		}

		if ( _timeSinceVolumeChanged < VolumeChangeDebounceSeconds )
		{
			return;
		}

		if ( _timeSinceLastWebReload < WebReloadMinIntervalSeconds )
		{
			return;
		}

		_lastAppliedEffectiveVolume = _lastObservedEffectiveVolume;
		ReloadWebUrlForPlayback( _lastObservedEffectiveVolume );
	}

	private async Task UnmuteAfterDelay( long nonce )
	{
		await GameTask.DelayRealtimeSeconds( WebUnmuteDelaySeconds );
		if ( nonce != _webUrlNonce )
		{
			return;
		}

		while ( nonce == _webUrlNonce && _timeSinceVolumeChanged < VolumeChangeDebounceSeconds )
		{
			await GameTask.DelayRealtimeSeconds( 0.05f );
		}
		if ( nonce != _webUrlNonce )
		{
			return;
		}

		if ( !_isCurrentlyPlaying || !Web.WebPanel.IsValid() )
		{
			return;
		}

		Web.WebPanel.Surface.TellMouseButton( MouseButtons.Left, true );
		Web.WebPanel.Surface.TellMouseButton( MouseButtons.Left, false );
	}

	public bool Press( IPressable.Event e )
	{
		_isTvTurnedOn = !_isTvTurnedOn;

		if ( !_isTvTurnedOn )
		{
			StopPlayback();
		}
		else if ( ShouldUpdatePlayback() )
		{
			UpdatePlaybackState();
		}

		Notify.Success( string.Format( Language.GetPhrase( "entity.tv.toggled" ), Language.GetPhrase( _isTvTurnedOn ? "entity.tv.on" : "entity.tv.off" ) ) );
		return true;
	}

	public override bool CanScale( Player player )
	{
		return HasTvPermission( player );
	}
}
