namespace Dxura.RP.Game.UI;

public static class TabMenuSectionPreferences
{
	private const string PreferencesPath = "tabmenu-collapsed-sections.json";

	private static TabMenuCollapsedSectionsDto? _cache;

	public static HashSet<Guid> LoadCollapsedJobGroups()
	{
		return Load().JobGroups
			.Select( id => Guid.TryParse( id, out var groupId ) ? groupId : Guid.Empty )
			.Where( id => id != Guid.Empty )
			.ToHashSet();
	}

	public static HashSet<string> LoadCollapsedMarketCategories()
	{
		return Load().MarketCategories.ToHashSet( StringComparer.OrdinalIgnoreCase );
	}

	public static bool LoadMarketAdminMode()
	{
		return Load().MarketAdminMode;
	}

	public static void SaveMarketAdminMode( bool enabled )
	{
		var data = Load();
		data.MarketAdminMode = enabled;
		Write( data );
	}

	public static void SaveCollapsedJobGroups( IEnumerable<Guid> collapsedGroupIds )
	{
		var data = Load();
		data.JobGroups = collapsedGroupIds
			.Where( id => id != Guid.Empty )
			.Select( id => id.ToString() )
			.Distinct()
			.ToList();
		Write( data );
	}

	public static void SaveCollapsedMarketCategories( IEnumerable<string> collapsedCategories )
	{
		var data = Load();
		data.MarketCategories = collapsedCategories
			.Where( category => !string.IsNullOrWhiteSpace( category ) )
			.Distinct( StringComparer.OrdinalIgnoreCase )
			.ToList();
		Write( data );
	}

	private static TabMenuCollapsedSectionsDto Load()
	{
		if ( _cache != null )
		{
			return _cache;
		}

		if ( !FileSystem.OrganizationData.FileExists( PreferencesPath ) )
		{
			_cache = new TabMenuCollapsedSectionsDto();
			return _cache;
		}

		try
		{
			var json = FileSystem.OrganizationData.ReadAllText( PreferencesPath );
			_cache = global::System.Text.Json.JsonSerializer.Deserialize<TabMenuCollapsedSectionsDto>( json ) ?? new TabMenuCollapsedSectionsDto();
		}
		catch ( Exception ex )
		{
			Log.Warning( $"Failed to load tab menu section preferences: {ex.Message}" );
			_cache = new TabMenuCollapsedSectionsDto();
		}

		return _cache;
	}

	private static void Write( TabMenuCollapsedSectionsDto data )
	{
		_cache = data;

		var json = global::System.Text.Json.JsonSerializer.Serialize( data );
		FileSystem.OrganizationData.WriteAllText( PreferencesPath, json );
	}

	private sealed class TabMenuCollapsedSectionsDto
	{
		public List<string> JobGroups { get; set; } = [];
		public List<string> MarketCategories { get; set; } = [];
		public bool MarketAdminMode { get; set; } = false;
	}
}
