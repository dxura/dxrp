using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public readonly record struct YouTubeVideoMetadata( string Title, int DurationSeconds );

public static class YouTubeApiClient
{
	private sealed class OEmbedResponse
	{
		[JsonPropertyName( "title" )]
		public string? Title { get; set; }

		[JsonPropertyName( "author_name" )]
		public string? AuthorName { get; set; }

		[JsonPropertyName( "thumbnail_url" )]
		public string? ThumbnailUrl { get; set; }
	}

	public static async Task<YouTubeVideoMetadata?> TryGetMetadata( string videoId )
	{
		if ( string.IsNullOrWhiteSpace( videoId ) )
		{
			return null;
		}

		return await ApiClientBase.SafeApiCall<YouTubeVideoMetadata?>( async () =>
			{
				// YouTube oEmbed: no API key required
				var url = $"https://www.youtube.com/oembed?url={Uri.EscapeDataString( $"https://www.youtube.com/watch?v={videoId}" )}&format=json";
				var resp = await ApiClientBase.RequestJsonAsync<OEmbedResponse>( url, "GET" );

				if ( resp == null )
				{
					return null;
				}

				var title = (resp.Title ?? string.Empty).Trim();
				if ( title.Length > 120 )
				{
					title = title[..120];
				}

				var duration = await TryGetDurationSeconds( videoId );

				return new YouTubeVideoMetadata( title, duration );
			},
			"Failed to get YouTube video metadata",
			logErrors: false );
	}

	private static async Task<int> TryGetDurationSeconds( string videoId )
	{
		var duration = await ApiClientBase.SafeApiCall( async () =>
			{
				var watchUrl = $"https://www.youtube.com/watch?v={Uri.EscapeDataString( videoId )}";
				var response = await ApiClientBase.RequestAsync( watchUrl );
				response.EnsureSuccessStatusCode();
				var html = await response.Content.ReadAsStringAsync();

				if ( string.IsNullOrEmpty( html ) )
				{
					return 0;
				}

				// Extract "lengthSeconds":"NNN" from the embedded player data
				var match = Regex.Match( html, @"""lengthSeconds""\s*:\s*""(\d+)""" );
				if ( match.Success && int.TryParse( match.Groups[1].Value, out var seconds ) )
				{
					return seconds;
				}

				return 0;
			},
			"Failed to get YouTube video duration",
			logErrors: false );

		return duration;
	}
}
