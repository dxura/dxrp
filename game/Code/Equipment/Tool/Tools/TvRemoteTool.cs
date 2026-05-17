using System.Web;
using Dxura.RP.Game.Entities;

namespace Dxura.RP.Game.Tools;

public readonly record struct YouTubePlaybackTarget( string? VideoId, string? PlaylistId );

[Tool( "#tool.tvremote.name", "#tool.tvremote.description", "#tool.group.interaction" )]
public class TvRemoteTool : BaseTool
{
	[Property]
	[Title( "Youtube (URL, playlist, or ID)" )]
	[Range( 0, 500 )]
	public string Video { get; set; } = "dQw4w9WgXcQ";

	[Property]
	[Title( "Video Offset (seconds)" )]
	[Range( 0, 10000 )]
	public string VideoOffset { get; set; } = "0";

	public override string Attack1Control => "#tool.tvremote.attack1";
	public override string Attack2Control => "#tool.tvremote.attack2";

	public override void PrimaryUseStart()
	{
		var playbackTarget = ParseYouTubePlaybackTarget( Video );
		if ( playbackTarget == null )
		{
			Notify.Error( "#notify.video.invalid" );
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "tv", Config.Current.Game.TvCooldown, true ) )
		{
			return;
		}

		if ( !int.TryParse( VideoOffset, out var videoOffset ) || videoOffset < 0 || videoOffset > 10000 )
		{
			Notify.Error( "#tool.tvremote.invalid_offset" );
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit || tr.GameObject.Tags.HasAny( Constants.PlayerTag, Constants.GrabbedTag, Constants.MapTag ) )
		{
			return;
		}

		var tv = tr.GameObject.Root.GetComponent<TvEntity>();

		if ( !tv.IsValid() )
		{
			Notify.Error( "#notify.tv.none" );
			return;
		}

		if ( !tv.HasTvPermission( Player.Local ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );

		tv.PlayYoutubeHost( playbackTarget.Value.VideoId ?? string.Empty, playbackTarget.Value.PlaylistId ?? string.Empty, videoOffset );
	}

	public override void SecondaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tv", Config.Current.Game.TvCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit || tr.GameObject.Tags.HasAny( Constants.PlayerTag, Constants.GrabbedTag, Constants.MapTag ) )
		{
			return;
		}

		var tv = tr.GameObject.Root.GetComponent<TvEntity>();

		if ( !tv.IsValid() )
		{
			Notify.Warn( "#notify.tv.none" );
			return;
		}

		if ( !tv.HasTvPermission( Player.Local ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		tv.StopHost();
		Notify.Success( "#notify.tv.reset" );
	}



	/// <summary>
	///     Extracts YouTube video ID from a URL or returns the provided ID if it's already in the correct format.
	/// </summary>
	/// <param name="input">YouTube URL or video ID</param>
	/// <returns>The YouTube video ID or null if input is invalid</returns>
	public static string? GetYouTubeVideoId( string input )
	{
		return ParseYouTubePlaybackTarget( input )?.VideoId;
	}

	public static YouTubePlaybackTarget? ParseYouTubePlaybackTarget( string input )
	{
		if ( string.IsNullOrWhiteSpace( input ) )
		{
			return null;
		}

		var trimmedInput = input.Trim();

		// Case 1: Input is already a video ID (11 characters)
		if ( trimmedInput.Length == 11 && !trimmedInput.Contains( "/" ) && !trimmedInput.Contains( "?" ) )
		{
			return new YouTubePlaybackTarget( trimmedInput, null );
		}

		// Case 2: Extract from URL
		try
		{
			if ( !Uri.TryCreate( trimmedInput, UriKind.Absolute, out var uri ) )
			{
				return null;
			}

			var query = HttpUtility.ParseQueryString( uri.Query );
			var playlistId = query["list"];

			// Handle various URL formats:
			// - https://www.youtube.com/watch?v=VIDEO_ID
			// - https://www.youtube.com/playlist?list=PLAYLIST_ID
			// - https://youtu.be/VIDEO_ID
			// - https://www.youtube.com/embed/VIDEO_ID
			// - https://www.youtube.com/v/VIDEO_ID
			// - https://www.youtube.com/shorts/VIDEO_ID

			// Standard watch URL with query parameters
			if ( trimmedInput.Contains( "youtube.com/watch" ) || trimmedInput.Contains( "youtube.com/playlist" ) )
			{
				var videoId = query["v"];
				if ( !string.IsNullOrEmpty( videoId ) && videoId.Length == 11 )
				{
					return new YouTubePlaybackTarget( videoId, playlistId );
				}

				if ( !string.IsNullOrEmpty( playlistId ) )
				{
					return new YouTubePlaybackTarget( null, playlistId );
				}
			}

			// Short URL format (youtu.be)
			else if ( trimmedInput.Contains( "youtu.be/" ) )
			{
				var path = uri.AbsolutePath.TrimStart( '/' );
				if ( path.Length >= 11 )
				{
					return new YouTubePlaybackTarget( path.Substring( 0, 11 ), playlistId );
				}
			}

			// Embed, v/ or shorts URL format
			else if ( trimmedInput.Contains( "youtube.com/embed/" ) ||
			          trimmedInput.Contains( "youtube.com/v/" ) ||
			          trimmedInput.Contains( "youtube.com/shorts/" ) )
			{
				var path = uri.AbsolutePath;
				var segments = path.Split( new[]
				{
					'/'
				}, StringSplitOptions.RemoveEmptyEntries );
				if ( segments.Length > 0 && segments[segments.Length - 1].Length >= 11 )
				{
					return new YouTubePlaybackTarget( segments[segments.Length - 1].Substring( 0, 11 ), playlistId );
				}
			}
		}
		catch
		{
			// Invalid URL format
			return null;
		}

		return null;
	}
}
