using System.Linq;

namespace Dxura.RP.Game;

public static class FrameImageCache
{
	private const int MaxCachedImages = 64;
	private static readonly object CacheLock = new();
	private static readonly Dictionary<string, byte[]> Cache = new();

	public static bool TryGet( string url, out byte[] imageData )
	{
		lock ( CacheLock )
		{
			return Cache.TryGetValue( url, out imageData! );
		}
	}

	public static void Set( string url, byte[] imageData )
	{
		lock ( CacheLock )
		{
			Cache[url] = imageData;

			if ( Cache.Count <= MaxCachedImages )
			{
				return;
			}

			var oldestKey = Cache.Keys.FirstOrDefault();
			if ( oldestKey != null )
			{
				Cache.Remove( oldestKey );
			}
		}
	}
}
