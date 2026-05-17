using Dxura.RP.Game.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Dxura.RP.Game;

public record struct CinemaQueueItem
{
	public string VideoId { get; set; }
	public string Title { get; set; }
	public int DurationSeconds { get; set; }
	public long RequestedBy { get; set; }
}

public record struct NowPlayingInfo
{
	public string? VideoId { get; set; }
	public string? Title { get; set; }
	public int DurationSeconds { get; set; }
	public long RequestedBy { get; set; }
}

public record struct SkipVoteInfo
{
	public int Eligible { get; set; }
	public int Needed { get; set; }
}

// Map-placed cinema controller: not a spawnable entity resource.
// Attach this component to a trigger collider to define the "cinema zone".
public sealed class Cinema : Component, Component.ITriggerListener, IGameEvents
{
	private const int MaxQueueItems = 50;
	private const float WebUnmuteDelaySeconds = 2.5f;
	private const float WebReloadMinIntervalSeconds = 3.0f;
	private const float VolumeChangeDebounceSeconds = 0.6f;
	private const float ZonePlayerValidationIntervalSeconds = 2.0f;

	[Property] public string DisplayName { get; set; } = "Cinema";
	[Property] public required Web Web { get; set; }

	[Sync( SyncFlags.FromHost )] private TimeSince? TimeSinceVideoPlay { get; set; }
	[Sync( SyncFlags.FromHost )] private NowPlayingInfo NowPlaying { get; set; }
	[Sync( SyncFlags.FromHost )] private SkipVoteInfo SkipInfo { get; set; }
	[Sync( SyncFlags.FromHost )] private int Volume { get; set; } = 75;
	[Sync( SyncFlags.FromHost )] private int StateVersion { get; set; }

	[Sync( SyncFlags.FromHost )] private NetList<CinemaQueueItem> Queue { get; set; } = new();
	[Sync( SyncFlags.FromHost )] private NetDictionary<long, bool> SkipVotes { get; set; } = new();

	private readonly HashSet<long> _playersInVoteZone = new();
	private bool _localInVoteZone;
	private bool _isCurrentlyPlaying;
	private string? _lastPlayedVideoId;
	private int _lastAppliedEffectiveVolume = -1;
	private int _lastObservedEffectiveVolume = -1;
	private float _lastObservedCinemaMixer = -1f;
	private int _pendingEffectiveVolume = -1;
	private TimeSince _timeSinceVolumeChanged;
	private TimeSince _timeSinceLastWebReload;
	private TimeSince _timeSinceZonePlayerValidation;
	private long _webUrlNonce;

	/// <summary>
	/// The cinema instance the local player is currently inside, if any.
	/// </summary>
	public static Cinema? Local { get; private set; }

	public bool IsLocalInVoteZone => _localInVoteZone;
	public int SkipYesVotes => SkipVotes?.Count ?? 0;
	public int SkipNeeded => SkipInfo.Needed;
	public int Eligible => SkipInfo.Eligible;
	public int Version => StateVersion;
	public string? NowPlayingTitle => NowPlaying.Title;
	public long NowPlayingRequestedBy => NowPlaying.RequestedBy;
	public int NowPlayingDurationSeconds => NowPlaying.DurationSeconds;
	public float NowPlayingElapsedSeconds => TimeSinceVideoPlay.HasValue ? (float)TimeSinceVideoPlay.Value : 0f;
	public bool HasNowPlaying => !string.IsNullOrWhiteSpace( NowPlaying.VideoId ) && TimeSinceVideoPlay.HasValue;
	public IEnumerable<CinemaQueueItem> QueueItems => Queue;

	protected override void OnStart()
	{
		base.OnStart();
		StopPlaybackLocal();
		_timeSinceZonePlayerValidation = 999f;

		if ( Networking.IsHost )
		{
			QueueDefaultVideoOnStartHost();
		}
	}

	private void QueueDefaultVideoOnStartHost()
	{
		if ( HasNowPlaying || Queue.Count > 0 )
		{
			return;
		}

		Queue.Add( new CinemaQueueItem
		{
			VideoId = "d6sCwH0YTLg", Title = "Night of the Living Dead", DurationSeconds = 4860, RequestedBy = 0
		} );
		StateVersion++;
		StartNextFromQueueHost();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		SyncLocalCinemaVolume();
	}

	public void OnSecondlyUpdate()
	{
		ValidateZonePlayersHost();
		UpdatePlaybackState();
		CheckVideoFinishedHost();
	}

	private void ValidateZonePlayersHost()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( _timeSinceZonePlayerValidation < ZonePlayerValidationIntervalSeconds )
		{
			return;
		}
		_timeSinceZonePlayerValidation = 0;

		if ( _playersInVoteZone.Count == 0 )
		{
			return;
		}

		var removedAny = false;
		foreach ( var steamId in _playersInVoteZone.ToArray() )
		{
			var player = GameUtils.GetPlayerById( steamId );
			if ( !player.IsValid() || !player.IsConnected )
			{
				_playersInVoteZone.Remove( steamId );
				SkipVotes.Remove( steamId );
				removedAny = true;
			}
		}

		if ( removedAny )
		{
			UpdateSkipThresholdHost( checkForSkip: true );
			StateVersion++;
		}
	}

	private int GetEffectiveLocalVolume()
	{
		var cinemaMixer = DxSound.CinemaVolume.Clamp( 0f, 1f );
		var effectiveVolume = (int)Math.Round( Volume * cinemaMixer );
		return Math.Clamp( effectiveVolume, 0, 100 );
	}

	private void CheckVideoFinishedHost()
	{
		if ( !Networking.IsHost || !HasNowPlaying || NowPlaying.DurationSeconds <= 0 )
		{
			return;
		}

		if ( TimeSinceVideoPlay!.Value >= NowPlaying.DurationSeconds + 2f )
		{
			SkipToNextInternalHost();
		}
	}

	private void UpdatePlaybackState()
	{
		if ( !_localInVoteZone || !HasNowPlaying )
		{
			if ( _isCurrentlyPlaying )
			{
				StopPlaybackLocal();
			}
			return;
		}

		if ( !_isCurrentlyPlaying )
		{
			StartPlaybackLocal();
		}
		else if ( NowPlaying.VideoId != _lastPlayedVideoId )
		{
			StopPlaybackLocal();
			StartPlaybackLocal();
		}
	}

	private void StartPlaybackLocal()
	{
		_isCurrentlyPlaying = true;
		_lastPlayedVideoId = NowPlaying.VideoId;
		Web.GameObject.Enabled = true;
		PlayVideoLocal();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastStopPlayback()
	{
		StopPlaybackLocal();
	}

	private void StopPlaybackLocal()
	{
		_isCurrentlyPlaying = false;
		_lastPlayedVideoId = null;
		_lastAppliedEffectiveVolume = -1;
		_lastObservedEffectiveVolume = -1;
		_lastObservedCinemaMixer = -1f;
		_pendingEffectiveVolume = -1;
		_webUrlNonce++; // invalidate any pending unmute tasks
		_timeSinceLastWebReload = 999f;
		if ( Web.IsValid() )
		{
			Web.GameObject.Enabled = false;
		}
	}

	private void ReloadWebUrlForPlayback( int effectiveVolume )
	{
		if ( !Web.IsValid() || !HasNowPlaying )
		{
			return;
		}

		_webUrlNonce++;
		_timeSinceLastWebReload = 0;

		var start = (int)TimeSinceVideoPlay!.Value;
		Web.Url = $"https://dxrp.net/embed/youtube?videoId={Uri.EscapeDataString( NowPlaying.VideoId ?? string.Empty )}&volume={effectiveVolume}&start={start}";
		_ = UnmuteAfterDelay( _webUrlNonce );
	}

	private void PlayVideoLocal()
	{
		if ( !HasNowPlaying )
		{
			return;
		}

		var effectiveVolume = GetEffectiveLocalVolume();
		_lastObservedCinemaMixer = DxSound.CinemaVolume.Clamp( 0f, 1f );
		_lastAppliedEffectiveVolume = effectiveVolume;
		_lastObservedEffectiveVolume = effectiveVolume;
		_pendingEffectiveVolume = effectiveVolume;
		_timeSinceVolumeChanged = 0;
		_timeSinceLastWebReload = 999f;
		ReloadWebUrlForPlayback( effectiveVolume );
	}

	private void SyncLocalCinemaVolume()
	{
		if ( !_isCurrentlyPlaying || !_localInVoteZone || !HasNowPlaying || !Web.IsValid() )
		{
			return;
		}

		var cinemaMixer = DxSound.CinemaVolume.Clamp( 0f, 1f );
		var effectiveVolume = GetEffectiveLocalVolume();

		// Debounce based on the slider value itself (not only the rounded effective volume)
		// so we don't reload mid-drag when the int volume doesn't change.
		const float mixerEpsilon = 0.0001f;
		if ( MathF.Abs( cinemaMixer - _lastObservedCinemaMixer ) > mixerEpsilon )
		{
			_lastObservedCinemaMixer = cinemaMixer;
			_lastObservedEffectiveVolume = effectiveVolume;
			_pendingEffectiveVolume = effectiveVolume;
			_timeSinceVolumeChanged = 0;
		}
		else if ( effectiveVolume != _lastObservedEffectiveVolume )
		{
			_lastObservedEffectiveVolume = effectiveVolume;
			_pendingEffectiveVolume = effectiveVolume;
			_timeSinceVolumeChanged = 0;
		}

		if ( _pendingEffectiveVolume < 0 || _pendingEffectiveVolume == _lastAppliedEffectiveVolume )
		{
			return;
		}

		if ( _timeSinceVolumeChanged < VolumeChangeDebounceSeconds )
		{
			return;
		}

		// Avoid spamming reloads; too many rapid URL changes can cause the embed to stop progressing.
		if ( _timeSinceLastWebReload < WebReloadMinIntervalSeconds )
		{
			return;
		}

		_lastAppliedEffectiveVolume = _pendingEffectiveVolume;
		ReloadWebUrlForPlayback( _pendingEffectiveVolume );
	}

	private async Task UnmuteAfterDelay( long nonce )
	{
		await GameTask.DelayRealtimeSeconds( WebUnmuteDelaySeconds );
		if ( nonce != _webUrlNonce )
		{
			return;
		}

		// If the user is still dragging the volume slider, wait until it settles so we only unmute once.
		while ( nonce == _webUrlNonce && _timeSinceVolumeChanged < VolumeChangeDebounceSeconds )
		{
			await GameTask.DelayRealtimeSeconds( 0.05f );
		}
		if ( nonce != _webUrlNonce )
		{
			return;
		}

		if ( !_isCurrentlyPlaying || !_localInVoteZone )
		{
			return;
		}

		if ( !Web.WebPanel.IsValid() )
		{
			return;
		}

		Web.WebPanel.Surface.TellMouseButton( MouseButtons.Left, true );
		Web.WebPanel.Surface.TellMouseButton( MouseButtons.Left, false );
	}

	private static bool IsValidYouTubeVideoId( string videoId )
	{
		if ( string.IsNullOrWhiteSpace( videoId ) )
		{
			return false;
		}

		if ( videoId.Length != 11 )
		{
			return false;
		}

		foreach ( var c in videoId )
		{
			if ( !char.IsLetterOrDigit( c ) && c != '-' && c != '_' )
			{
				return false;
			}
		}

		return true;
	}

	[Rpc.Host]
	public void QueueVideoHost( string input )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:cinema:queue", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !_playersInVoteZone.Contains( player.SteamId ) )
		{
			player.Error( "You must be in the cinema area to queue." );
			return;
		}

		var videoId = TvRemoteTool.GetYouTubeVideoId( input );
		if ( videoId == null || !IsValidYouTubeVideoId( videoId ) )
		{
			player.Error( "Invalid video" );
			return;
		}

		if ( Queue.Count >= MaxQueueItems )
		{
			player.Error( "Queue is full" );
			return;
		}

		_ = QueueVideoInternal( player, videoId );
	}

	private async Task QueueVideoInternal( Player player, string videoId )
	{
		await GameTask.WorkerThread();
		var meta = await YouTubeApiClient.TryGetMetadata( videoId );
		await GameTask.MainThread();

		if ( !this.IsValid() || !player.IsValid() )
		{
			return;
		}

		var item = new CinemaQueueItem
		{
			VideoId = videoId, Title = meta?.Title ?? $"YouTube ({videoId})", DurationSeconds = meta?.DurationSeconds ?? 0, RequestedBy = player.SteamId
		};

		Queue.Add( item );
		StateVersion++;

		if ( !HasNowPlaying && Networking.IsHost )
		{
			StartNextFromQueueHost();
		}
	}

	[Rpc.Host]
	public void RemoveFromQueueHost( int index )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:cinema:remove", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !_playersInVoteZone.Contains( player.SteamId ) )
		{
			player.Error( "You must be in the cinema area to manage the queue." );
			return;
		}

		if ( index < 0 || index >= Queue.Count )
		{
			return;
		}

		var item = Queue[index];
		if ( item.RequestedBy != player.SteamId )
		{
			player.Error( "No permission" );
			return;
		}

		Queue.RemoveAt( index );
		StateVersion++;
	}

	private void StartNextFromQueueHost()
	{
		if ( Queue.Count <= 0 )
		{
			return;
		}

		var item = Queue[0];
		Queue.RemoveAt( 0 );
		StartVideoHost( item );
	}

	private void StartVideoHost( CinemaQueueItem item )
	{
		BroadcastStopPlayback();

		TimeSinceVideoPlay = 0;
		NowPlaying = new NowPlayingInfo
		{
			VideoId = item.VideoId, Title = item.Title, DurationSeconds = item.DurationSeconds, RequestedBy = item.RequestedBy
		};
		SkipVotes.Clear();
		SkipInfo = new SkipVoteInfo
		{
			Eligible = _playersInVoteZone.Count, Needed = CalculateSkipThreshold( _playersInVoteZone.Count )
		};
		StateVersion++;
	}

	[Rpc.Host]
	public void VoteSkipHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:cinema:skipvote", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		if ( !HasNowPlaying )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !_playersInVoteZone.Contains( player.SteamId ) )
		{
			player.Error( "You must be in the cinema area to vote." );
			return;
		}

		if ( !SkipVotes.TryAdd( player.SteamId, true ) )
		{
			return;
		}

		UpdateSkipThresholdHost( checkForSkip: true );
		StateVersion++;
	}

	private void SkipToNextInternalHost()
	{
		if ( Queue.Count > 0 )
		{
			StartNextFromQueueHost();
			return;
		}

		NowPlaying = default;
		TimeSinceVideoPlay = null;
		SkipVotes.Clear();
		SkipInfo = default;
		StateVersion++;
		BroadcastStopPlayback();
	}

	private static int CalculateSkipThreshold( int eligible )
	{
		if ( eligible <= 0 )
		{
			return 0;
		}
		// Simple majority.
		var needed = eligible / 2 + 1;
		return Math.Clamp( needed, 1, eligible );
	}

	private void UpdateSkipThresholdHost( bool checkForSkip = false )
	{
		var eligible = _playersInVoteZone.Count;
		SkipInfo = new SkipVoteInfo
		{
			Eligible = eligible, Needed = CalculateSkipThreshold( eligible )
		};

		if ( checkForSkip && HasNowPlaying && SkipInfo.Needed > 0 && SkipVotes.Count >= SkipInfo.Needed )
		{
			SkipToNextInternalHost();
		}
	}

	public void OnTriggerEnter( GameObject other )
	{
		var player = other.Root.GetComponent<Player>();
		if ( player == Player.Local )
		{
			_localInVoteZone = true;
			Local = this;
			UpdatePlaybackState();
		}

		if ( !Networking.IsHost || !player.IsValid() )
		{
			return;
		}

		if ( _playersInVoteZone.Add( player.SteamId ) )
		{
			UpdateSkipThresholdHost( checkForSkip: true );
			StateVersion++;
		}
	}

	public void OnTriggerExit( GameObject other )
	{
		var player = other.Root.GetComponent<Player>();
		if ( player == Player.Local )
		{
			_localInVoteZone = false;
			if ( Local == this )
			{
				Local = null;
			}
			StopPlaybackLocal();
		}

		if ( !Networking.IsHost || !player.IsValid() )
		{
			return;
		}

		if ( _playersInVoteZone.Remove( player.SteamId ) )
		{
			SkipVotes.Remove( player.SteamId );
			UpdateSkipThresholdHost( checkForSkip: true );
			StateVersion++;
		}
	}

	public void OnPlayerDisconnectHost( long steamId )
	{
		_playersInVoteZone.Remove( steamId );
		SkipVotes.Remove( steamId );
		UpdateSkipThresholdHost( checkForSkip: true );
		StateVersion++;
	}
}
