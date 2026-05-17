using Dxura.RP.Shared;

namespace Dxura.RP.Game;

public static class GameModeEntityDtoExtensions
{
	public static GameModeAddonContentDto? Content( this GameModeEntityDto? dto )
	{
		return dto == null ? null : GameModeAddonContents.FindById( dto.GameModeAddonContentId );
	}

	public static string Identifier( this GameModeEntityDto? dto )
	{
		return GameModeAddonContents.GetLookupKey( dto.Content() );
	}

	public static string PrefabPath( this GameModeEntityDto? dto )
	{
		return dto.Content()?.PrimaryReference ?? string.Empty;
	}

	public static string Grouping( this GameModeEntityDto? dto )
	{
		return dto.Content()?.Grouping ?? string.Empty;
	}

	public static string Name( this GameModeEntityDto? dto )
	{
		return dto?.NameOverride ?? dto.Content()?.Name ?? string.Empty;
	}

	public static string Description( this GameModeEntityDto? dto )
	{
		return dto?.DescriptionOverride ?? dto.Content()?.Description ?? string.Empty;
	}

	public static string DisplayName( this GameModeEntityDto? dto )
	{
		if ( dto == null )
		{
			return string.Empty;
		}

		var name = dto.Name();
		if ( !string.IsNullOrEmpty( name ) ) return name;

		var identifier = dto.Identifier();
		return !string.IsNullOrEmpty( identifier ) ? identifier : dto.PrefabPath();
	}

	public static string DisplayDescription( this GameModeEntityDto? dto )
	{
		return dto?.Description() ?? string.Empty;
	}

	public static bool IsValid( this GameModeEntityDto? dto )
	{
		return dto != null && !string.IsNullOrWhiteSpace( dto.Identifier() );
	}
}
