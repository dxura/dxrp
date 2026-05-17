using System.Threading;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public class Frame() : BaseConstruct( ConstructType.Frame ), IDescription
{
	private const int MaxPhotoSize = 1024 * 1024 * 4; // 4MB max size
	private const int MaxRetries = 3;
	private const int RetryDelayMs = 5000;

	[Property]
	public required ModelRenderer PictureRenderer { get; set; }
	[Property]
	public required ModelRenderer FrameRenderer { get; set; }
	[Property]
	public required Material PictureMaterial { get; set; }

	private Texture _frameTexture = Texture.Invalid;
	private Material? _frameMaterialCopy;
	private CancellationTokenSource? _imageLoadCts;

	private FrameData _frameData = new();
	private bool _hasSetTexture;
	private string? _loadingUrl;

	public override void OnUnoccluded()
	{
		TryStartPhotoLoad();
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		var oldFrameData = oldData is FrameData oldDataT ? oldDataT : default;
		var newFrameData = newData is FrameData newDataT ? newDataT : default;
		_frameData = newFrameData;

		if ( oldFrameData.ImgurUrl != newFrameData.ImgurUrl )
		{
			_hasSetTexture = false;
			TryStartPhotoLoad();
		}

		WorldScale = new Vector3( newFrameData.Size.x, newFrameData.Size.y, 1f );

		ApplyFrameVisualSettings( newFrameData );

		if ( Networking.IsHost )
		{
			var player = GameUtils.GetPlayerById( Owner );
			if ( player.IsValid() )
			{
				_ = ServerApiClient.Audit( "Frame", $"{player.SteamName} ({player.SteamId}) {newFrameData.ImgurUrl}.", player.SteamId );
			}
		}
	}

	private void ApplyFrameVisualSettings( FrameData frameData )
	{
		if ( !FrameRenderer.IsValid() )
		{
			return;
		}

		FrameRenderer.Tint = frameData.FrameColor;
		FrameRenderer.Enabled = frameData.FrameEnabled;
	}

	private void TryStartPhotoLoad()
	{
		OcclusionSystem.Current.ForceCheckGameObject( GameObject );

		if ( GameObject.Tags.Has( Constants.OccludeTag ) )
		{
			return;
		}

		if ( !_hasSetTexture )
		{
			if ( _loadingUrl == _frameData.ImgurUrl )
			{
				return;
			}

			StartPhotoLoad( _frameData.ImgurUrl );
		}
	}

	private void StartPhotoLoad( string url )
	{
		_loadingUrl = url;
		CancelImageLoad();
		_imageLoadCts = new CancellationTokenSource();
		_ = SetPhotoFromUrl( url, _imageLoadCts.Token );
	}

	private void CancelImageLoad()
	{
		_imageLoadCts?.Cancel();
		_imageLoadCts?.Dispose();
	}

	private void CreatePhotoMaterial( byte[] photo )
	{
		var bitmap = Bitmap.CreateFromBytes( photo );

		var texture = bitmap?.ToTexture();

		if ( texture == null )
		{
			return;
		}

		if ( !PictureMaterial.IsValid() || !PictureRenderer.IsValid() )
		{
			texture.Dispose();
			return;
		}

		if ( _frameMaterialCopy == null )
		{
			_frameMaterialCopy = PictureMaterial.CreateCopy();
		}

		_frameMaterialCopy.Set( "Color", texture );
		PictureRenderer.SetMaterialOverride( _frameMaterialCopy, "" );

		_frameTexture?.Dispose();
		_frameTexture = texture;
		_hasSetTexture = true;
		_loadingUrl = null;
	}

	private async Task SetPhotoFromUrl( string url, CancellationToken cancellationToken )
	{
		if ( GameManager.IsHeadless || string.IsNullOrWhiteSpace( url ) )
		{
			_loadingUrl = null;
			return;
		}

		url = ResolveFrameImageUrl( url );

		for ( var attempt = 0; attempt <= MaxRetries; attempt++ )
		{
			if ( cancellationToken.IsCancellationRequested )
			{
				return;
			}

			var imageData = await FetchImageFromUrl( url, cancellationToken );

			await GameTask.MainThread();

			if ( cancellationToken.IsCancellationRequested || !GameObject.IsValid() || _hasSetTexture )
			{
				return;
			}

			if ( imageData != null )
			{
				CreatePhotoMaterial( imageData );
				return;
			}

			if ( attempt < MaxRetries )
			{
				Log.Warning( $"Frame failed to load '{url}', retrying ({attempt + 1}/{MaxRetries})..." );
				await GameTask.Delay( RetryDelayMs );
			}
		}

		Log.Warning( $"Frame failed to load '{url}' after {MaxRetries} retries." );
		_hasSetTexture = false;
		if ( _loadingUrl == _frameData.ImgurUrl )
		{
			_loadingUrl = null;
		}
	}

	private static string ResolveFrameImageUrl( string url )
	{
		if ( !DxProxyFrames )
		{
			return url;
		}

		var uri = new Uri( url );
		return $"https://imgur.dxrp.net{uri.AbsolutePath}";
	}

	private static async Task<byte[]?> FetchImageFromUrl( string url, CancellationToken cancellationToken )
	{
		if ( FrameImageCache.TryGet( url, out var cachedImageData ) )
		{
			return cachedImageData;
		}

		try
		{
			var imageData = await Http.RequestBytesAsync( url );
			if ( cancellationToken.IsCancellationRequested )
			{
				return null;
			}

			if ( imageData.Length > MaxPhotoSize )
			{
				return null;
			}

			FrameImageCache.Set( url, imageData );
			return imageData;
		}
		catch ( Exception )
		{
			return null;
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		CancelImageLoad();
		_frameTexture?.Dispose();
		_frameMaterialCopy = null;
	}
	
	[ConVar( "dx_proxy_frames", ConVarFlags.Saved )]
	private static bool DxProxyFrames { get; set; } = false;
}


