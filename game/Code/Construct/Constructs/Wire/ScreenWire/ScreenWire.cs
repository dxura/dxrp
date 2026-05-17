using System.Threading.Tasks;

namespace Dxura.RP.Game.Wire;

enum DisplayMode
{
	Text,
	Camera
}

[Title( "Screen" )]
[Category( "Wire" )]
[Icon( "monitor" )]
public class ScreenWire() : BaseWireConstruct( ConstructType.ScreenWire ), IWireEvents
{
	private ScreenWireData _data = new();

	[Property]
	[Change( nameof( OnCurrentValueChanged ) )]
	private string CurrentValue { get; set; } = "...";

	public override string Name => $"Screen ({_data.Label})";

	private DisplayMode _mode = DisplayMode.Text;

	private bool _playerWithinRaycast;

	private CameraWire? _cachedCameraWire;

	private Texture? _cameraTexture; // render target used for camera
	private byte[]? _cachedHeaderBytesCamera;

	private Texture? _backgroundTexture; // static background with header (for text-mode or fallback)
	private Material? _displayMaterialCopy; // cached material copy to prevent GPU leak

	public override Vector3 GetPortPosition()
	{
		return GameObject.WorldPosition + WorldRotation.Backward * (_data.Height * 0.5f);
	}

	[Property]
	public ModelRenderer BackingRenderer { get; set; } = null!;

	[Property]
	public ModelRenderer DisplayRenderer { get; set; } = null!;

	[Property]
	public TextRenderer ValueTextRenderer { get; set; } = null!;

	[WireInput( "value" )]
	public object? Value
	{
		set
		{
			var newValue = value?.ToString() ?? "...";
			if ( CurrentValue != newValue )
			{
				BroadcastScreenValue( newValue );
			}
		}
		get => CurrentValue;
	}

	protected override void OnStart()
	{
		base.OnStart();

		ReflectValue();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( GameManager.IsHeadless )
		{
			return;
		}
		if ( _mode != DisplayMode.Camera )
		{
			return;
		}
		if ( GameObject.Tags.Has( Constants.OccludeTag ) )
		{
			return;
		}

		if ( !Cooldown.Current.CheckAndStartCooldown( $"camera:{Id}", Config.Current.Game.WireScreenCameraRaycastInterval ) )
		{
			if ( !RenderWireScreenCamera )
			{
				_playerWithinRaycast = false;
			}
			else
			{
				RaycastPlayerCheck();
			}
		}

		if ( !_playerWithinRaycast )
		{
			return;
		}

		// Try to find camera if we don't have it yet
		if ( !Cooldown.Current.CheckAndStartCooldown( CurrentValue, Config.Current.Game.ActionCooldown ) && !_cachedCameraWire.IsValid() )
		{
			_cachedCameraWire = Sandbox.Game.ActiveScene.GetAllComponents<CameraWire>()
				.FirstOrDefault( x => x.Identifier == CurrentValue );
		}

		RenderCamera();
	}

	private void RaycastPlayerCheck()
	{
		var rayStart = Scene.Camera.WorldPosition;
		var rayEnd = GameObject.WorldPosition;
		var tr = Scene.Trace.Ray( rayStart, rayEnd )
			.WithoutTags( "invisible", "trigger", Constants.OccludeTag, Constants.PlayerTag )
			.UseHitboxes()
			.Run();

		_playerWithinRaycast = tr.GameObject == GameObject;
	}

	private void OnCurrentValueChanged( string oldValue, string newValue )
	{
		ReflectValue();
	}

	private void ReflectValue( bool forceRefresh = false )
	{
		if ( GameManager.IsHeadless )
		{
			return;
		}

		var previousMode = _mode;
		_mode = CurrentValue.StartsWith( CameraWireDefinition.CameraPrefix ) ? DisplayMode.Camera : DisplayMode.Text;

		switch ( _mode )
		{
			case DisplayMode.Text:
				if ( previousMode == DisplayMode.Camera || forceRefresh )
				{
					ValueTextRenderer.Enabled = true;
					CleanupCameraTexture();
					_ = CreateTextBackgroundTexture();
				}

				UpdateValueText();
				break;

			case DisplayMode.Camera:
				if ( previousMode == DisplayMode.Text || forceRefresh )
				{
					ValueTextRenderer.Enabled = false;
					CleanupCameraTexture();
					CreateCameraTexture();
				}

				_cachedCameraWire = Sandbox.Game.ActiveScene.GetAllComponents<CameraWire>()
					.FirstOrDefault( x => x.Identifier == CurrentValue );

				// Try render immediately (safe)
				RenderCamera();
				break;
		}

		// Update the material to show the current display texture (text or camera)
		UpdateScreenTexture();
	}

	private void RenderCamera()
	{
		if ( GameManager.IsHeadless || !RenderWireScreenCamera )
		{
			return;
		}
		if ( !_cachedCameraWire.IsValid() )
		{
			return;
		}
		if ( _cameraTexture == null )
		{
			return;
		}

		// Limit to 10FPS camera updates
		if ( Cooldown.Current.CheckAndStartCooldown( $"camera:render:{Id}", 0.10f ) )
		{
			return;
		}

		// Render the camera to the camera texture
		if ( _cachedCameraWire.Camera.IsValid() )
		{
			_cachedCameraWire.Camera.RenderToTexture( _cameraTexture );
		}

		// Upload cached header after camera render (RenderToTexture may clear texture)
		if ( _cachedHeaderBytesCamera == null || !_data.ShowHeader )
		{
			return;
		}

		var textureWidth = _cameraTexture.Size.x;
		var textureHeight = _cameraTexture.Size.y;
		var headerHeight = (int)(textureHeight * 0.15f);

		// Update top area of the camera texture with header bytes
		_cameraTexture.Update( _cachedHeaderBytesCamera, 0, 0, (int)textureWidth, headerHeight );

		// Ensure DisplayRenderer material is using the camera texture
		UpdateScreenTexture();
	}

	private void CleanupCameraTexture()
	{
		try
		{
			_cameraTexture?.Dispose();
		}
		catch
		{
			/* ignore disposal errors */
		}
		_cameraTexture = null;
		_cachedHeaderBytesCamera = null;
	}

	// Rendering helpers
	private void DrawHeader( Bitmap bitmap, int width, int height )
	{
		// Fill header background
		for ( var y = 0; y < height; y++ )
		{
			for ( var x = 0; x < width; x++ )
			{
				bitmap.SetPixel( x, y, _data.HeaderColor );
			}
		}

		// Draw centered label
		if ( string.IsNullOrEmpty( _data.Label ) || width <= 0 || height <= 0 )
		{
			return;
		}

		var labelScope = new TextRendering.Scope( _data.Label, Color.White, 40f, "Poppins" );
		var textSize = labelScope.Measure();

		if ( !(textSize.x > 0) || !(textSize.y > 0) )
		{
			return;
		}

		var textX = (width - textSize.x) / 2f;
		var textY = (height - textSize.y) / 2f;
		bitmap.DrawText( labelScope, new Rect( textX, textY, textSize.x, textSize.y ) );
	}

	private async Task CreateTextBackgroundTexture()
	{
		if ( GameManager.IsHeadless )
		{
			return;
		}

		await GameTask.WorkerThread();

		var size = 512;
		var aspectRatio = (float)_data.Width / _data.Height;
		var textureWidth = Math.Clamp( (int)(size * aspectRatio), 1, 4096 );
		var bitmap = new Bitmap( textureWidth, size );

		bitmap.Clear( Color.Black );

		if ( _data.ShowHeader )
		{
			var headerHeight = (int)(size * 0.15f);
			DrawHeader( bitmap, textureWidth, headerHeight );
		}

		var newTexture = bitmap.ToTexture();

		await GameTask.MainThread();

		if ( !GameObject.IsValid() )
		{
			newTexture?.Dispose();
			return;
		}

		var old = _backgroundTexture;
		_backgroundTexture = newTexture;
		old?.Dispose();

		UpdateScreenTexture();
	}

	private void UpdateValueText()
	{
		if ( GameManager.IsHeadless )
		{
			return;
		}

		ValueTextRenderer.Enabled = true;

		var headerHeight = _data.ShowHeader ? _data.Height * 0.15f : 0;
		var remainingHeight = _data.Height - headerHeight;
		var scale = Math.Min( _data.Width, remainingHeight ) * 0.0045f;
		var maxWidth = _data.Width * 0.9f;
		var wrappedText = WrapText( CurrentValue, maxWidth, "Poppins", ScreenWireDefinition.ScreenDefaultFontSize * scale );

		ValueTextRenderer.Text = wrappedText;
		ValueTextRenderer.Color = Color.White;
		ValueTextRenderer.FontFamily = "Poppins";
		ValueTextRenderer.FontSize = ScreenWireDefinition.ScreenDefaultFontSize;
		ValueTextRenderer.HorizontalAlignment = TextRenderer.HAlignment.Center;
		ValueTextRenderer.VerticalAlignment = TextRenderer.VAlignment.Center;
		ValueTextRenderer.Scale = scale;
	}

	private void UpdateScreenTexture()
	{
		var originalMaterial = DisplayRenderer.Model?.Materials.FirstOrDefault();
		if ( originalMaterial == null )
		{
			return;
		}

		var displayTexture = GetActiveDisplayTexture();
		if ( displayTexture == null )
		{
			return;
		}

		if ( _displayMaterialCopy == null )
		{
			_displayMaterialCopy = originalMaterial.CreateCopy();
		}

		_displayMaterialCopy.Set( "Color", displayTexture );
		DisplayRenderer.SetMaterialOverride( _displayMaterialCopy, "" );
	}

	/// <summary>
	/// Returns which texture should be used for the display right now:
	/// - camera texture when in camera mode and available
	/// - background texture otherwise
	/// </summary>
	private Texture? GetActiveDisplayTexture()
	{
		if ( _mode == DisplayMode.Camera && _cameraTexture != null )
		{
			return _cameraTexture;
		}

		return _backgroundTexture;
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as ScreenWireData ?? new ScreenWireData();

		UpdateMeshes();

		ReflectValue( true );
	}

	private void CreateCameraTexture()
	{
		// Dispose legacy camera texture if present
		CleanupCameraTexture();

		var aspectRatio = (float)_data.Width / _data.Height;
		var textureWidth = Math.Clamp( (int)(ScreenWireDefinition.ScreenCameraTextureSize * aspectRatio), 1, 4096 );
		var textureHeight = Math.Clamp( ScreenWireDefinition.ScreenCameraTextureSize, 1, 4096 );

		_cameraTexture = Texture.CreateRenderTarget()
			.WithSize( textureWidth, textureHeight )
			.WithFormat( ImageFormat.RGBA8888 )
			.Create();

		// Generate header bytes if required
		if ( _data.ShowHeader )
		{
			var headerHeight = (int)(textureHeight * 0.15f);
			var headerBitmap = new Bitmap( textureWidth, headerHeight );
			DrawHeader( headerBitmap, textureWidth, headerHeight );

			var pixels = headerBitmap.GetPixels();
			_cachedHeaderBytesCamera = new byte[pixels.Length * 4];
			for ( var i = 0; i < pixels.Length; i++ )
			{
				var pixel = pixels[i];
				_cachedHeaderBytesCamera[i * 4] = (byte)(pixel.r * 255);
				_cachedHeaderBytesCamera[i * 4 + 1] = (byte)(pixel.g * 255);
				_cachedHeaderBytesCamera[i * 4 + 2] = (byte)(pixel.b * 255);
				_cachedHeaderBytesCamera[i * 4 + 3] = (byte)(pixel.a * 255);
			}
		}
		else
		{
			_cachedHeaderBytesCamera = null;
		}
	}

	// Mesh and geometry
	private void UpdateMeshes()
	{
		if ( BackingRenderer.IsValid() )
		{
			var boxMesh = CreateBoxMesh( _data.Width, _data.Height, ScreenWireDefinition.ScreenBackingThickness );
			BackingRenderer.Model = Model.Builder.AddMesh( boxMesh ).Create();
		}

		if ( DisplayRenderer.IsValid() )
		{
			var quadMesh = CreateQuadMesh( _data.Width, _data.Height );
			DisplayRenderer.Model = Model.Builder.AddMesh( quadMesh ).Create();

			// Model changed, so the cached material copy is no longer valid
			_displayMaterialCopy = null;
		}

		var collider = GameObject.GetComponent<BoxCollider>();
		if ( collider != null )
		{
			collider.Scale = new Vector3( _data.Height, _data.Width, ScreenWireDefinition.ScreenBackingThickness );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastScreenValue( string value )
	{
		CurrentValue = value;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		CleanupCameraTexture();
		_backgroundTexture?.Dispose();
		_backgroundTexture = null;
		_displayMaterialCopy = null;
	}

	private Mesh CreateBoxMesh( float width, float height, float thickness )
	{
		var halfWidth = width * 0.5f;
		var halfHeight = height * 0.5f;
		var halfThickness = thickness * 0.5f;

		var boxVertices = new Vertex[]
		{
			// Front face
			new()
			{
				Position = new Vector3( -halfHeight, -halfWidth, halfThickness ), Normal = Vector3.Forward
			},
			new()
			{
				Position = new Vector3( halfHeight, -halfWidth, halfThickness ), Normal = Vector3.Forward
			},
			new()
			{
				Position = new Vector3( halfHeight, halfWidth, halfThickness ), Normal = Vector3.Forward
			},
			new()
			{
				Position = new Vector3( -halfHeight, halfWidth, halfThickness ), Normal = Vector3.Forward
			},

			// Back face
			new()
			{
				Position = new Vector3( -halfHeight, -halfWidth, -halfThickness ), Normal = Vector3.Backward
			},
			new()
			{
				Position = new Vector3( halfHeight, -halfWidth, -halfThickness ), Normal = Vector3.Backward
			},
			new()
			{
				Position = new Vector3( halfHeight, halfWidth, -halfThickness ), Normal = Vector3.Backward
			},
			new()
			{
				Position = new Vector3( -halfHeight, halfWidth, -halfThickness ), Normal = Vector3.Backward
			}
		};

		var boxIndices = new int[]
		{
			// Front face
			0,
			1,
			2,
			0,
			2,
			3,
			// Back face
			5,
			4,
			7,
			5,
			7,
			6,
			// Left face
			4,
			0,
			3,
			4,
			3,
			7,
			// Right face
			1,
			5,
			6,
			1,
			6,
			2,
			// Top face
			3,
			2,
			6,
			3,
			6,
			7,
			// Bottom face
			4,
			5,
			1,
			4,
			1,
			0
		};

		var material = Material.Load( "materials/default.vmat" );
		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer<Vertex>( boxVertices.Length, Vertex.Layout, boxVertices );
		mesh.CreateIndexBuffer( boxIndices.Length, boxIndices );
		mesh.Bounds = new BBox( new Vector3( -halfHeight, -halfWidth, -halfThickness ), new Vector3( halfHeight, halfWidth, halfThickness ) );

		return mesh;
	}

	private Mesh CreateQuadMesh( float width, float height )
	{
		var halfWidth = width * 0.5f;
		var halfHeight = height * 0.5f;

		var quadVertices = new Vertex[]
		{
			new()
			{
				Position = new Vector3( -halfHeight, -halfWidth, ScreenWireDefinition.ScreenDisplayOffset ), Normal = Vector3.Forward, Tangent = new Vector4( Vector3.Right, 1.0f ), TexCoord0 = new Vector2( 1, 1 ) // Bottom-left
			},
			new()
			{
				Position = new Vector3( halfHeight, -halfWidth, ScreenWireDefinition.ScreenDisplayOffset ), Normal = Vector3.Forward, Tangent = new Vector4( Vector3.Right, 1.0f ), TexCoord0 = new Vector2( 1, 0 ) // Bottom-right
			},
			new()
			{
				Position = new Vector3( halfHeight, halfWidth, ScreenWireDefinition.ScreenDisplayOffset ), Normal = Vector3.Forward, Tangent = new Vector4( Vector3.Right, 1.0f ), TexCoord0 = new Vector2( 0, 0 ) // Top-right
			},
			new()
			{
				Position = new Vector3( -halfHeight, halfWidth, ScreenWireDefinition.ScreenDisplayOffset ), Normal = Vector3.Forward, Tangent = new Vector4( Vector3.Right, 1.0f ), TexCoord0 = new Vector2( 0, 1 ) // Top-left
			}
		};

		var quadIndices = new int[]
		{
			0,
			1,
			2,
			0,
			2,
			3
		};

		var material = Material.Load( "materials/screen.vmat" );

		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer<Vertex>( quadVertices.Length, Vertex.Layout, quadVertices );
		mesh.CreateIndexBuffer( quadIndices.Length, quadIndices );
		mesh.Bounds = new BBox( new Vector3( -halfHeight, -halfWidth, ScreenWireDefinition.ScreenDisplayOffset ), new Vector3( halfHeight, halfWidth, ScreenWireDefinition.ScreenDisplayOffset ) );

		return mesh;
	}

	// Wrapping text function
	private string WrapText( string text, float maxWidth, string fontFamily, float fontSize )
	{
		if ( string.IsNullOrEmpty( text ) )
		{
			return string.Empty;
		}

		var words = text.Split( ' ' );
		var currentLine = "";
		var result = "";

		foreach ( var word in words )
		{
			var testLine = string.IsNullOrEmpty( currentLine ) ? word : currentLine + " " + word;

			var scope = new TextRendering.Scope(
				testLine,
				Color.White,
				fontSize,
				fontFamily
			);
			var size = scope.Measure();

			if ( size.x > maxWidth )
			{
				if ( string.IsNullOrEmpty( currentLine ) )
				{
					// If a word is too long, it will be shortcut with "..."
					result += TruncateWord( word, maxWidth, fontFamily, fontSize ) + "\n";
				}
				else
				{
					// Manage the end of the line
					result += currentLine + "\n";
					currentLine = word;

					// Check if the first word of the raw is not too long
					var wordScope = new TextRendering.Scope( word, Color.White, fontSize, fontFamily );
					if ( wordScope.Measure().x > maxWidth )
					{
						currentLine = TruncateWord( word, maxWidth, fontFamily, fontSize );
					}
				}
			}
			else
			{
				currentLine = testLine;
			}
		}

		if ( !string.IsNullOrEmpty( currentLine ) )
		{
			result += currentLine;
		}

		return result;
	}

	// Function to shortcut a word if too long (add "..." at the end of it)
	private string TruncateWord( string word, float maxWidth, string fontFamily, float fontSize )
	{
		var dots = "...";
		var dotsScope = new TextRendering.Scope( dots, Color.White, fontSize, fontFamily );
		var dotsWidth = dotsScope.Measure().x;

		var truncated = "";
		foreach ( var c in word )
		{
			var test = truncated + c + dots;
			var scope = new TextRendering.Scope( test, Color.White, fontSize, fontFamily );
			var width = scope.Measure().x;

			if ( width > maxWidth )
			{
				break;
			}

			truncated += c;
		}

		return truncated + dots;
	}

	[ConVar( "dx_wire_screen_camera", ConVarFlags.Saved )]
	private static bool RenderWireScreenCamera { get; set; } = true;
}
