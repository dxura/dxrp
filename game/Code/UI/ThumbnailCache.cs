using System.Threading.Tasks;

namespace Dxura.RP.Game;

public static class ThumbnailCache
{
	private static readonly Dictionary<Model, Texture> ModelCache = new();
	private static readonly Dictionary<Material, Texture> MaterialCache = new();

	private static readonly Dictionary<string, Texture?> UrlCache = new();
	private static readonly LinkedList<string> UrlOrder = new();
	private static readonly HashSet<string> UrlLoading = new();
	private static readonly HashSet<string> UrlFailed = new();
	private static readonly Dictionary<string, List<Action>> UrlWaiters = new();

	private const int MaxUrlCacheSize = 500;
	private const int MaxUrlBytes = 1024 * 1024 * 2;

	public static void Clear()
	{
		foreach ( var texture in ModelCache.Values )
			texture?.Dispose();

		foreach ( var texture in MaterialCache.Values )
			texture?.Dispose();

		foreach ( var texture in UrlCache.Values )
			texture?.Dispose();

		ModelCache.Clear();
		MaterialCache.Clear();
		UrlCache.Clear();
		UrlOrder.Clear();
		UrlLoading.Clear();
		UrlFailed.Clear();
		UrlWaiters.Clear();
	}

	public static Texture Get( Model model )
	{
		if ( ModelCache.TryGetValue( model, out var tex ) )
			return tex;

		return GenerateTexture( model );
	}

	public static Texture Get( Material material )
	{
		if ( MaterialCache.TryGetValue( material, out var tex ) )
			return tex;

		return GenerateTexture( material );
	}

	/// <summary>
	/// Returns the cached texture for a URL, or null if not yet loaded.
	/// Pass onLoaded to register a callback fired once when the texture becomes available.
	/// Passing the same callback on subsequent calls is safe — it is only registered once per load.
	/// </summary>
	public static Texture? GetUrl( string? url, Action? onLoaded = null )
	{
		if ( string.IsNullOrWhiteSpace( url ) )
			return null;

		if ( UrlCache.TryGetValue( url, out var cached ) )
			return cached;

		if ( UrlFailed.Contains( url ) )
			return null;

		if ( onLoaded != null )
		{
			if ( !UrlWaiters.TryGetValue( url, out var list ) )
			{
				list = new List<Action>();
				UrlWaiters[url] = list;
			}

			list.Add( onLoaded );
		}

		if ( !UrlLoading.Contains( url ) )
		{
			UrlLoading.Add( url );
			_ = LoadUrlAsync( url );
		}

		return null;
	}

	private static async Task LoadUrlAsync( string url )
	{
		try
		{
			var bytes = await Http.RequestBytesAsync( url );

			if ( bytes.Length <= 0 || bytes.Length > MaxUrlBytes )
			{
				MarkUrlFailed( url );
				return;
			}

			await GameTask.MainThread();

			var texture = Bitmap.CreateFromBytes( bytes )?.ToTexture();
			if ( texture == null )
			{
				MarkUrlFailed( url );
				return;
			}

			if ( UrlCache.Count >= MaxUrlCacheSize )
			{
				var oldest = UrlOrder.First!.Value;
				UrlOrder.RemoveFirst();
				if ( UrlCache.TryGetValue( oldest, out var old ) )
				{
					old?.Dispose();
					UrlCache.Remove( oldest );
				}
			}

			UrlCache[url] = texture;
			UrlOrder.AddLast( url );
			UrlLoading.Remove( url );
			FireWaiters( url );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"ThumbnailCache: failed to load URL ({url}): {ex.Message}" );
			MarkUrlFailed( url );
		}
	}

	private static void MarkUrlFailed( string url )
	{
		UrlLoading.Remove( url );
		UrlFailed.Add( url );
		FireWaiters( url );
	}

	private static void FireWaiters( string url )
	{
		if ( !UrlWaiters.TryGetValue( url, out var waiters ) )
			return;

		UrlWaiters.Remove( url );
		foreach ( var cb in waiters )
			cb();
	}

	private static Texture GenerateTexture( Model? model )
	{
		if ( model is null || model.IsError )
		{
			if ( model != null )
				ModelCache[model] = Texture.Invalid;

			return Texture.Transparent;
		}

		var texture = Texture.CreateRenderTarget().WithSize( 128, 128 ).Create();
		var scene = new Scene();
		using ( scene.Push() )
		{
			var modelGo = new GameObject();
			var modelRenderer = modelGo.AddComponent<ModelRenderer>();
			modelRenderer.Model = model;

			var cameraGo = new GameObject();
			var camera = cameraGo.AddComponent<CameraComponent>();
			camera.FieldOfView = 50;
			camera.BackgroundColor = Color.Transparent;

			var bounds = model.Bounds;
			var center = bounds.Center;
			var distance = bounds.Size.Length * 1.3f;
			var lightRadius = MathF.Max( bounds.Size.Length * 2.5f, 100f );

			cameraGo.WorldRotation = Rotation.From( 25, -45, 0 );
			cameraGo.WorldPosition = center + cameraGo.WorldRotation.Backward * distance;

			var lightGo = new GameObject();
			lightGo.WorldPosition = cameraGo.WorldPosition;
			lightGo.WorldRotation = Rotation.LookAt( center - lightGo.WorldPosition );

			var spotLight = lightGo.AddComponent<SpotLight>();
			spotLight.LightColor = Color.White * 3.0f;
			spotLight.Radius = lightRadius;
			spotLight.Attenuation = 0.5f;
			spotLight.ConeOuter = 60f;
			spotLight.Shadows = false;

			camera.RenderToTexture( texture );
		}

		ModelCache[model] = texture;
		return texture;
	}

	private static Texture GenerateTexture( Material? material )
	{
		if ( !material.IsValid() )
		{
			if ( material != null )
				MaterialCache[material] = Texture.Invalid;

			return Texture.Transparent;
		}

		var texture = Texture.CreateRenderTarget().WithSize( 128, 128 ).Create();
		var scene = new Scene();
		using ( scene.Push() )
		{
			var modelGo = new GameObject();
			var modelRenderer = modelGo.AddComponent<ModelRenderer>();
			modelRenderer.Model = Model.Sphere;
			modelRenderer.SetMaterialOverride( material, "" );

			var cameraGo = new GameObject();
			var camera = cameraGo.AddComponent<CameraComponent>();
			camera.FieldOfView = 60;
			camera.BackgroundColor = new Color( 0.15f, 0.15f, 0.15f );

			cameraGo.WorldPosition = new Vector3( 45, 15, 45 );
			cameraGo.WorldRotation = Rotation.LookAt( Vector3.Zero - cameraGo.WorldPosition );

			var mainLight = new GameObject();
			var directLight = mainLight.AddComponent<DirectionalLight>();
			mainLight.WorldRotation = Rotation.From( 60, -60, 0 );
			directLight.LightColor = Color.White * 1.5f;

			camera.RenderToTexture( texture );
		}

		MaterialCache[material] = texture;
		return texture;
	}

	[ConCmd( "dx_clear_thumbnail_cache" )]
	public static void ToggleSoundScape()
	{
		Clear();
	}
}
