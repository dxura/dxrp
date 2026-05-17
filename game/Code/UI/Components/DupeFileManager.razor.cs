using Dxura.RP.Game.Tools;
using Sandbox.Modals;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dxura.RP.Game.UI;

public partial class DupeFileManager
{
	private TextEntry? _dupeNameText;
	private TextEntry? _renameText;
	private TextEntry? _searchText;
	private string? _renamingFile;
	private List<string> _files = new();
	private readonly HashSet<string> _openFolders = new( StringComparer.OrdinalIgnoreCase );

	private string SearchQuery => _searchText?.Value?.Trim() ?? "";

	private IEnumerable<string> FilteredFiles => string.IsNullOrWhiteSpace( SearchQuery )
		? _files
		: _files.Where( f => f.Replace( DupeFileExtension, "" ).Contains( SearchQuery, StringComparison.OrdinalIgnoreCase ) );

	const string DupeFileExtension = ".dupe";
	const string EncryptionKey = "LeaveMyDupesAloneDXRP123";

	protected override void OnAfterTreeRender( bool firstTime )
	{
		base.OnAfterTreeRender( firstTime );
		_files = FileSystem.OrganizationData.FindFile( "dupes", "*" + DupeFileExtension, true ).OrderBy( x => x ).ToList();
	}

	private IEnumerable<DupeBrowserEntry> BrowserEntries => string.IsNullOrWhiteSpace( SearchQuery )
		? GetBrowserEntries( "", 0 )
		: FilteredFiles.Select( file => DupeBrowserEntry.File( file, 0 ) );

	private void OnFileSelected( string file )
	{
		var path = "dupes/" + file;
		var json = FileSystem.OrganizationData.ReadAllText( path );

		var decryptedJson = DecryptDupe( json );
		var dupe = JsonSerializer.Deserialize<ConstructDupe>( decryptedJson );

		if (dupe == null)
		{
			Notify.Error( "#generic.error" );
			return;
		}

		dupe.Name = GetDupeDisplayName( file );

		_dupeNameText?.Blur();

		DuplicatorTool.SelectedDupe = dupe;
		DuplicatorTool.SelectedDupeSaved = true;
		Notify.Success( "#notify.dupe.loaded" );
	}

	private void OnFileUpdate( string file )
	{
		var dupeName = file.Replace( DupeFileExtension, "" );
		SaveDupe( name: dupeName );

		if ( DuplicatorTool.SelectedDupe != null )
		{
			DuplicatorTool.SelectedDupe.Name = GetDupeDisplayName( file );
			DuplicatorTool.SelectedDupeSaved = true;
		}
	}

	private void OnFileDelete( string file )
	{
		try
		{
			FileSystem.OrganizationData.DeleteFile( "dupes/" + file );
		}
		catch ( Exception )
		{
			Notify.Error( "#generic.error" );
			return;
		}

		Notify.Success( "#notify.dupe.deleted" );

		var deletedName = GetDupeDisplayName( file );
		if ( DuplicatorTool.SelectedDupe?.Name == deletedName )
		{
			DuplicatorTool.SelectedDupe = null;
		}

		DuplicatorTool.SelectedDupeSaved = false;
	}

	protected override int BuildHash()
	{
		return HashCode.Combine(DuplicatorTool.SelectedDupe, DuplicatorTool.SelectedDupeSaved, SearchQuery);
	}

	public static void SaveDupe(ConstructDupe? dupe = null, string? name = null)
	{
		dupe ??= DuplicatorTool.SelectedDupe;

		if(dupe == null) return;

		if ( !FileSystem.OrganizationData.DirectoryExists( "dupes" ) )
		{
			FileSystem.OrganizationData.CreateDirectory( "dupes" );
		}

		var dupeName = name;
		if(string.IsNullOrEmpty( dupeName ))
		{
			dupeName = DateTime.Now.ToString( "yyyyMMdd_HHmmss" );
		}
		else if ( !IsValidDupePath( dupeName ) )
		{
			Notify.Error("#tool.duplicator.dupes.validation");
			return;
		}

		try
		{
			var directory = GetDirectory( dupeName );
			if ( !string.IsNullOrEmpty( directory ) && !FileSystem.OrganizationData.DirectoryExists( "dupes/" + directory ) )
			{
				FileSystem.OrganizationData.CreateDirectory( "dupes/" + directory );
			}

			var json = JsonSerializer.Serialize( dupe );
			var encryptedJson = EncryptDupe( json);
			FileSystem.OrganizationData.WriteAllText( "dupes/" + dupeName + DupeFileExtension, encryptedJson );
		}
		catch ( Exception )
		{
			Notify.Error( "#generic.error" );
			return;
		}

		Notify.Success( "#notify.dupe.saved" );
	}

	private void DiscardDupe()
	{
		_dupeNameText?.Blur();
		DuplicatorTool.SelectedDupe = null;
		DuplicatorTool.SelectedDupeSaved = true;

		Notify.Success( "#notify.dupe.discarded" );
	}

	private static string EncryptDupe( string json)
	{
		var keyBytes = Encoding.UTF8.GetBytes( EncryptionKey.PadRight( 32 ).Substring( 0, 32 ) );
		var plainBytes = Encoding.UTF8.GetBytes( json );

		for ( var i = 0; i < plainBytes.Length; i++ )
		{
			plainBytes[i] ^= keyBytes[i % keyBytes.Length];
		}

		return Encoding.UTF8.GetString( plainBytes );
	}

	private static string DecryptDupe( string encryptedJson)
	{
		var keyBytes = Encoding.UTF8.GetBytes( EncryptionKey.PadRight( 32 ).Substring( 0, 32 ) );
		var encryptedBytes = Encoding.UTF8.GetBytes( encryptedJson );

		for ( var i = 0; i < encryptedBytes.Length; i++ )
		{
			encryptedBytes[i] ^= keyBytes[i % keyBytes.Length];
		}

		return Encoding.UTF8.GetString( encryptedBytes );
	}
	
	private void OnFileRename( string file )
	{
		_renamingFile = file;
		StateHasChanged();
	}

	private void ConfirmRename( string file )
	{
		var newName = _renameText?.Value?.Trim();
		var oldName = GetDupeDisplayName( file );

		_renamingFile = null;
		_renameText = null;

		if ( string.IsNullOrWhiteSpace( newName ) || newName == oldName )
		{
			StateHasChanged();
			return;
		}

		if ( !IsValidDupePath( newName ) )
		{
			Notify.Error( "#tool.duplicator.dupes.validation" );
			return;
		}

		var oldPath = "dupes/" + file;
		var newPath = "dupes/" + newName + DupeFileExtension;

		if ( FileSystem.OrganizationData.FileExists( newPath ) )
		{
			Notify.Error( Language.GetPhrase( "dupe.name_exists" ) );
			return;
		}

		try
		{
			var directory = GetDirectory( newName );
			if ( !string.IsNullOrEmpty( directory ) && !FileSystem.OrganizationData.DirectoryExists( "dupes/" + directory ) )
			{
				FileSystem.OrganizationData.CreateDirectory( "dupes/" + directory );
			}

			var contents = FileSystem.OrganizationData.ReadAllText( oldPath );
			FileSystem.OrganizationData.WriteAllText( newPath, contents );
			FileSystem.OrganizationData.DeleteFile( oldPath );
		}
		catch ( Exception )
		{
			Notify.Error( "#generic.error" );
			return;
		}

		if ( DuplicatorTool.SelectedDupe?.Name == oldName )
		{
			DuplicatorTool.SelectedDupe.Name = GetFileName( newName );
		}

		Notify.Success( Language.GetPhrase( "dupe.renamed" ) );
		StateHasChanged();
	}

	private void CancelRename()
	{
		_renamingFile = null;
		_renameText = null;
		StateHasChanged();
	}

	private void OnFileShare( string file )
	{
		_ = GameTask.RunInThreadAsync( () => ShareDupeToWorkshop( file ) );
	}

	private async Task ShareDupeToWorkshop( string file )
	{
		var dupeName = GetDupeDisplayName( file );
		string? errorMessage = null;

		Texture? texture = null;
		GameObject? tempGo = null;
		Bitmap? bitmap = null;

		await GameTask.MainThread();
		var sceneCamera = Sandbox.Game.ActiveScene.Camera;
		if ( !sceneCamera.IsValid() )
		{
			return;
		}

		try
		{
			texture = Texture.CreateRenderTarget().WithSize( 1000, 1000 ).Create();

			// Create a temporary camera that matches the player's view but won't render UI/viewmodel.
			// We wait a tick before and after rendering so the render actually lands in the RT.
			tempGo = new GameObject
			{
				WorldPosition = sceneCamera.WorldPosition,
				WorldRotation = sceneCamera.WorldRotation
			};

			var tempCamera = tempGo.AddComponent<CameraComponent>();
			tempCamera.IsMainCamera = false;
			tempCamera.FieldOfView = sceneCamera.FieldOfView;
			tempCamera.BackgroundColor = sceneCamera.BackgroundColor;
			tempCamera.ZNear = sceneCamera.ZNear;
			tempCamera.ZFar = sceneCamera.ZFar;
			tempCamera.RenderExcludeTags = ["ui", "viewmodel", "player", "preview"];

			await Task.FixedUpdate();
			tempCamera.RenderToTexture( texture );
			await Task.FixedUpdate();

			await GameTask.WorkerThread();
			bitmap = texture.GetBitmap( 0 );

			await GameTask.MainThread();

			var dupe = Storage.CreateEntry( Config.Current.Game.DupeWorkshopType );
			dupe.SetThumbnail( bitmap );
			dupe.SetMeta( "package", Sandbox.Game.Ident );

			var dupeJson = await FileSystem.OrganizationData.ReadAllTextAsync( "dupes/" + file );
			if ( string.IsNullOrEmpty( dupeJson ) )
			{
				throw new Exception( "Dupe file is empty" );
			}
			
			dupe.Files.WriteAllText( "dupe.json", dupeJson );

			dupe.Publish( new WorkshopPublishOptions
			{
				Title = dupeName,
				CanSelectVisibility = true,
				// ReSharper disable once UseCollectionExpression
				Tags = new HashSet<string> { Config.Current.Game.DupeWorkshopType }
			} );
		}
		catch ( Exception ex )
		{
			errorMessage = ex.Message;
		}

		await GameTask.MainThread();
		tempGo?.Destroy();
		texture?.Dispose();
		bitmap?.Dispose();

		if ( errorMessage != null )
		{
			Log.Warning( $"Failed to share dupe '{dupeName}': {errorMessage}" );
			Notify.Error( "#generic.error" );
		}
	}

	private IEnumerable<DupeBrowserEntry> GetBrowserEntries( string folder, int depth )
	{
		var prefix = string.IsNullOrEmpty( folder ) ? "" : folder + "/";
		var folders = _files
			.Where( file => file.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) && file[prefix.Length..].Contains( '/' ) )
			.Select( file => file[prefix.Length..].Split( '/', 2 )[0] )
			.Distinct( StringComparer.OrdinalIgnoreCase )
			.OrderBy( x => x );

		foreach ( var childFolder in folders )
		{
			var path = prefix + childFolder;
			yield return DupeBrowserEntry.Folder( path, childFolder, depth, _openFolders.Contains( path ) );

			if ( _openFolders.Contains( path ) )
			{
				foreach ( var entry in GetBrowserEntries( path, depth + 1 ) )
				{
					yield return entry;
				}
			}
		}

		foreach ( var file in _files.Where( file => GetDirectory( file.Replace( DupeFileExtension, "" ) ) == folder ).OrderBy( x => x ) )
		{
			yield return DupeBrowserEntry.File( file, depth );
		}
	}

	private void ToggleFolder( string folder )
	{
		if ( !_openFolders.Add( folder ) )
		{
			_openFolders.Remove( folder );
		}
	}

	private static bool IsValidDupePath( string path )
	{
		var parts = path.Replace( '\\', '/' ).Split( '/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );
		return parts.Length > 0 && parts.All( part => part.All( c => char.IsLetterOrDigit( c ) || char.IsWhiteSpace( c ) || c is '-' or '_' ) );
	}

	private static string GetDirectory( string path )
	{
		path = path.Replace( '\\', '/' );
		var slash = path.LastIndexOf( '/' );
		return slash <= 0 ? "" : path[..slash];
	}

	private readonly record struct DupeBrowserEntry(
		bool IsFolder,
		string Path,
		string Name,
		int Depth,
		bool IsOpen )
	{
		public static DupeBrowserEntry Folder( string path, string name, int depth, bool isOpen )
		{
			return new DupeBrowserEntry( true, path, name, depth, isOpen );
		}

		public static DupeBrowserEntry File( string path, int depth )
		{
			return new DupeBrowserEntry( false, path, GetDupeDisplayName( path ), depth, false );
		}
	}

	private static string GetDupeDisplayName( string file )
	{
		return GetFileName( file.Replace( DupeFileExtension, "" ) );
	}

	private static string GetFileName( string path )
	{
		path = path.Replace( '\\', '/' );
		var slash = path.LastIndexOf( '/' );
		return slash < 0 ? path : path[(slash + 1)..];
	}
}
