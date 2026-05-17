using Dxura.RP.Shared;

namespace Dxura.RP.Game;

public static class GameModeEquipmentDtoExtensions
{
	private static readonly Dictionary<string, Model> ModelCache = new( StringComparer.OrdinalIgnoreCase );

	public static GameModeAddonContentDto? Content( this GameModeEquipmentDto? dto )
	{
		return dto == null ? null : GameModeAddonContents.FindById( dto.GameModeAddonContentId );
	}

	public static string Identifier( this GameModeEquipmentDto? dto )
	{
		return GameModeAddonContents.GetLookupKey( dto.Content() );
	}

	public static string PrefabPath( this GameModeEquipmentDto? dto )
	{
		return dto.Content()?.PrimaryReference ?? string.Empty;
	}

	public static string? SecondaryPrefabPath( this GameModeEquipmentDto? dto )
	{
		return dto.Content()?.SecondaryReference;
	}

	public static string Grouping( this GameModeEquipmentDto? dto )
	{
		return dto.Content()?.Grouping ?? string.Empty;
	}

	public static string Name( this GameModeEquipmentDto? dto )
	{
		return dto?.NameOverride ?? dto.Content()?.Name ?? string.Empty;
	}

	public static string Description( this GameModeEquipmentDto? dto )
	{
		return dto?.DescriptionOverride ?? dto.Content()?.Description ?? string.Empty;
	}

	public static EquipmentSlot SlotValue( this GameModeEquipmentDto? dto )
	{
		var grouping = dto.Grouping();
		if ( string.IsNullOrWhiteSpace( grouping ) )
		{
			return EquipmentSlot.Undefined;
		}

		return Enum.TryParse<EquipmentSlot>( grouping, true, out var slot ) ? slot : EquipmentSlot.Undefined;
	}

	public static string DisplayName( this GameModeEquipmentDto? dto )
	{
		if ( dto == null )
		{
			return string.Empty;
		}

		return dto.Name();
	}

	public static string DisplayDescription( this GameModeEquipmentDto? dto )
	{
		if ( dto == null )
		{
			return string.Empty;
		}

		return dto.Description();
	}

	public static string DisplayIcon( this GameModeEquipmentDto? dto )
	{
		return dto.Content()?.IconPath ?? string.Empty;
	}

	public static Model? GetWorldModel( this GameModeEquipmentDto? dto )
	{
		var modelPath = dto.Content()?.WorldModelPath;
		if ( string.IsNullOrWhiteSpace( modelPath ) )
		{
			return null;
		}

		if ( ModelCache.TryGetValue( modelPath, out var cached ) )
		{
			return cached;
		}

		cached = Model.Load( modelPath );
		ModelCache[modelPath] = cached;
		return cached;
	}

	public static float WorldModelScale( this GameModeEquipmentDto? dto )
	{
		return dto.Content()?.WorldModelScale ?? 1f;
	}

	public static bool IsValid( this GameModeEquipmentDto? dto )
	{
		return dto != null && !string.IsNullOrWhiteSpace( dto.Identifier() );
	}
}
