using Dxura.RP.Shared;

namespace Dxura.RP.Game;

public static class GameModeJobs
{
	private static readonly Dictionary<string, GameModeJobDto> PlaceholderJobs = new( StringComparer.OrdinalIgnoreCase );

	public static IReadOnlyList<GameModeJobDto> All => Config.Current.GameMode.Jobs;
	public static IReadOnlyList<GameModeJobGroupDto> AllGroups => Config.Current.GameMode.JobGroups;

	public static GameModeJobDto Default
	{
		get
		{
			var gm = Config.Current.GameMode;
			return FindById( gm.DefaultJobId )
				?? gm.Jobs.FirstOrDefault()
				?? GetByNameOrFallback( "Default" );
		}
	}

	public static GameModeJobDto? FindByName( string name )
	{
		if ( string.IsNullOrWhiteSpace( name ) )
		{
			return null;
		}

		var normalized = NormalizeKey( name );
		return All.FirstOrDefault( x => NormalizeKey( x.Name ) == normalized );
	}

	public static GameModeJobDto? FindByReference( string? value )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
		{
			return null;
		}

		if ( Guid.TryParse( value, out var id ) )
		{
			return FindById( id );
		}

		return FindByName( value );
	}

	public static GameModeJobDto GetByNameOrFallback( string name )
	{
		return FindByName( name ) ?? GetOrCreatePlaceholder( name );
	}

	public static GameModeJobDto? FindById( Guid? id )
	{
		if ( !id.HasValue )
		{
			return null;
		}

		return All.FirstOrDefault( x => x.Id == id.Value );
	}

	public static GameModeJobGroupDto? FindGroupById( Guid? id )
	{
		if ( !id.HasValue )
		{
			return null;
		}

		return AllGroups.FirstOrDefault( x => x.Id == id.Value );
	}

	public static GameModeJobGroupDto? FindGroupByReference( string? value )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
		{
			return null;
		}

		if ( Guid.TryParse( value, out var id ) )
		{
			return FindGroupById( id );
		}

		var normalized = NormalizeKey( value );
		return AllGroups.FirstOrDefault( x => NormalizeKey( x.Name ) == normalized );
	}

	public static GameModeJobDto[] GetPrerequisiteJobs( GameModeJobDto job )
	{
		var prerequisite = FindById( job.PrerequisiteJobId );
		return prerequisite == null ? [] : [prerequisite];
	}

	public static GameModeJobDto GetByTagOrFallback( JobTag tag, string fallbackName )
	{
		var tagged = All.FirstOrDefault( x => x.HasTag( tag ) );
		if ( tagged != null )
		{
			return tagged;
		}

		var mapped = JobTags.GetDefaultJobName( tag );
		if ( !string.IsNullOrWhiteSpace( mapped ) )
		{
			var mappedJob = FindByName( mapped );
			if ( mappedJob != null )
			{
				return mappedJob;
			}
		}

		return GetByNameOrFallback( fallbackName );
	}

	private static GameModeJobDto GetOrCreatePlaceholder( string name )
	{
		name = name?.Trim() ?? string.Empty;

		if ( PlaceholderJobs.TryGetValue( name, out var job ) )
		{
			return job;
		}

		job = new GameModeJobDto
		{
			Id = Guid.NewGuid(),
			GameModeJobGroupId = null,
			PrerequisiteJobId = null,
			Name = name,
			Description = string.Empty,
			Color = 0,
			Model = "models/citizen/citizen.vmdl",
			Clothes = [],
			JobTags = [],
			Salary = 0,
			IncludeDefaultEquipment = true,
			Health = 100,
			DemoteOnRespawn = false,
			Interaction = null,
			Selectable = true,
			MaxCount = 0,
			VoteRequired = false,
			ElectionRequired = false
		};

		PlaceholderJobs[name] = job;
		return job;
	}

	private static string NormalizeKey( string? value )
	{
		value ??= string.Empty;
		return new string( value
			.Where( c => c != ' ' && c != '_' && c != '-' )
			.Select( char.ToLowerInvariant )
			.ToArray() );
	}
}
