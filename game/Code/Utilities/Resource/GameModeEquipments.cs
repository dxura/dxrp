using Dxura.RP.Shared;

namespace Dxura.RP.Game;

public static class GameModeEquipments
{
	private static readonly Dictionary<string, GameModeEquipmentDto> PlaceholderEquipments = new( StringComparer.OrdinalIgnoreCase );

	public static readonly Guid ToolGameModeContentId = new( "37580dd6-fcbd-477a-99c2-5109c1da20d8" );

	public static IReadOnlyList<GameModeEquipmentDto> All => Config.Current.GameMode.Equipments;
	public static GameModeEquipmentDto Hands => GetByIdentifierOrFallback( "hands" );

	public static GameModeEquipmentDto? FindByIdentifier( string identifier )
	{
		if ( string.IsNullOrWhiteSpace( identifier ) )
		{
			return null;
		}

		return All.FirstOrDefault( x => string.Equals( x.Identifier(), identifier, StringComparison.OrdinalIgnoreCase ) );
	}

	public static GameModeEquipmentDto? FindById( Guid? id )
	{
		if ( !id.HasValue )
		{
			return null;
		}

		return All.FirstOrDefault( x => x.Id == id.Value );
	}

	public static bool IsTool( GameModeEquipmentDto? equipment )
	{
		return equipment?.GameModeAddonContentId == ToolGameModeContentId;
	}

	public static GameModeEquipmentDto? FindByPrefabPath( string prefabPath )
	{
		if ( string.IsNullOrWhiteSpace( prefabPath ) )
		{
			return null;
		}

		return All.FirstOrDefault( x => string.Equals( x.PrefabPath(), prefabPath, StringComparison.OrdinalIgnoreCase ) );
	}

	public static GameModeEquipmentDto GetByIdentifierOrFallback( string identifier )
	{
		return FindByIdentifier( identifier ) ?? GetOrCreatePlaceholder( identifier );
	}

	private static GameModeEquipmentDto GetOrCreatePlaceholder( string identifier )
	{
		identifier = identifier?.Trim() ?? string.Empty;

		if ( PlaceholderEquipments.TryGetValue( identifier, out var equipment ) )
		{
			return equipment;
		}

		var placeholderContent = GameModeAddonContents.GetOrCreatePlaceholder( identifier );
		equipment = new GameModeEquipmentDto
		{
			Id = Guid.NewGuid(),
			GameModeAddonContentId = placeholderContent.Id,
			NameOverride = null,
			DescriptionOverride = null
		};

		PlaceholderEquipments[identifier] = equipment;
		return equipment;
	}
}
