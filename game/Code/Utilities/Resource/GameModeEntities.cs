using Dxura.RP.Shared;

namespace Dxura.RP.Game;

public static class GameModeEntities
{
	private static readonly Dictionary<string, GameModeEntityDto> PlaceholderEntities = new( StringComparer.OrdinalIgnoreCase );

	public static IReadOnlyList<GameModeEntityDto> All => Config.Current.GameMode.Entities;

	public static GameModeEntityDto? FindByIdentifier( string identifier )
	{
		if ( string.IsNullOrWhiteSpace( identifier ) )
		{
			return null;
		}

		return All.FirstOrDefault( x => string.Equals( x.Identifier(), identifier, StringComparison.OrdinalIgnoreCase ) );
	}

	public static GameModeEntityDto? FindById( Guid? id )
	{
		if ( !id.HasValue )
		{
			return null;
		}

		return All.FirstOrDefault( x => x.Id == id.Value );
	}

	public static GameModeEntityDto? FindByPrefabPath( string prefabPath )
	{
		if ( string.IsNullOrWhiteSpace( prefabPath ) )
		{
			return null;
		}

		return All.FirstOrDefault( x => string.Equals( x.PrefabPath(), prefabPath, StringComparison.OrdinalIgnoreCase ) );
	}

	public static GameModeEntityDto GetByIdentifierOrFallback( string identifier )
	{
		return FindByIdentifier( identifier ) ?? GetOrCreatePlaceholder( identifier );
	}

	private static GameModeEntityDto GetOrCreatePlaceholder( string identifier )
	{
		identifier = identifier?.Trim() ?? string.Empty;

		if ( PlaceholderEntities.TryGetValue( identifier, out var entity ) )
		{
			return entity;
		}

		var placeholderContent = GameModeAddonContents.GetOrCreatePlaceholder( identifier );
		entity = new GameModeEntityDto
		{
			Id = Guid.NewGuid(),
			GameModeAddonContentId = placeholderContent.Id,
			NameOverride = null,
			DescriptionOverride = null
		};

		PlaceholderEntities[identifier] = entity;
		return entity;
	}
}
