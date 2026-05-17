using Dxura.RP.Shared;
using System.IO;

namespace Dxura.RP.Game;

public static class GameModeAddonContents
{
	private static readonly Dictionary<string, GameModeAddonContentDto> PlaceholderContents = new( StringComparer.OrdinalIgnoreCase );

	public static IEnumerable<GameModeAddonContentDto> All =>
		Config.Current.GameMode.Addons.SelectMany( addon => addon.Contents );

	public static GameModeAddonContentDto? FindById( Guid? id )
	{
		if ( !id.HasValue || id.Value == Guid.Empty )
		{
			return null;
		}

		var fromConfig = All.FirstOrDefault( content => content.Id == id.Value );
		if ( fromConfig != null )
		{
			return fromConfig;
		}

		return PlaceholderContents.Values.FirstOrDefault( content => content.Id == id.Value );
	}

	public static GameModeAddonContentDto? FindByIdentifier( string? identifier )
	{
		if ( string.IsNullOrWhiteSpace( identifier ) )
		{
			return null;
		}

		return All.FirstOrDefault( content => string.Equals( GetLookupKey( content ), identifier, StringComparison.OrdinalIgnoreCase ) );
	}

	public static GameModeAddonContentDto? FindByPrefabPath( string? prefabPath )
	{
		if ( string.IsNullOrWhiteSpace( prefabPath ) )
		{
			return null;
		}

		return All.FirstOrDefault( content => string.Equals( content.PrimaryReference, prefabPath, StringComparison.OrdinalIgnoreCase ) );
	}

	public static GameModeAddonContentDto GetOrCreatePlaceholder( string? identifier )
	{
		identifier = identifier?.Trim() ?? string.Empty;

		if ( PlaceholderContents.TryGetValue( identifier, out var content ) )
		{
			return content;
		}

		content = new GameModeAddonContentDto
		{
			Id = Guid.NewGuid(),
			AddonContentId = Guid.Empty,
			Name = identifier,
			Description = string.Empty,
			PrimaryReference = string.Empty,
			SecondaryReference = string.Empty,
			Grouping = string.Empty,
			IconPath = string.Empty,
			WorldModelPath = string.Empty,
			WorldModelScale = null
		};

		PlaceholderContents[identifier] = content;
		return content;
	}

	public static string GetLookupKey( GameModeAddonContentDto? content )
	{
		if ( content == null )
		{
			return string.Empty;
		}

		if ( !string.IsNullOrWhiteSpace( content.Name ) )
		{
			return content.Name;
		}

		var prefabPath = content.PrimaryReference;
		if ( string.IsNullOrWhiteSpace( prefabPath ) )
		{
			return string.Empty;
		}

		return Path.GetFileNameWithoutExtension( prefabPath ) ?? prefabPath;
	}
}
