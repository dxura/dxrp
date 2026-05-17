using Dxura.RP.Shared;

namespace Dxura.RP.Game;

[Flags]
public enum JobTag
{
	None = 0,
	Citizen = 1 << 0,
	PoliticalPrisoner = 1 << 1,
	Mayoral = 1 << 2,
	Police = 1 << 3,
	Chief = 1 << 4,
	Medic = 1 << 5,
	Government = 1 << 6,
	Hitman = 1 << 7,
	Sandbox = 1 << 8,
	Zombat = 1 << 9
}

public static class JobTags
{
	private static readonly Dictionary<string, JobTag> NamedTags = new( StringComparer.OrdinalIgnoreCase )
	{
		["citizen"] = JobTag.Citizen,
		["sandbox"] = JobTag.Sandbox,
		["zombat"] = JobTag.Zombat,
		["politicalprisoner"] = JobTag.PoliticalPrisoner,
		["mayor"] = JobTag.Mayoral | JobTag.Government,
		["medic"] = JobTag.Medic,
		["policeofficer"] = JobTag.Police | JobTag.Government,
		["policemedic"] = JobTag.Police | JobTag.Medic | JobTag.Government,
		["policechief"] = JobTag.Police | JobTag.Chief | JobTag.Government,
		["policesniper"] = JobTag.Police | JobTag.Government,
		["hitman"] = JobTag.Hitman
	};

	private static readonly Dictionary<string, JobTag> ExplicitTagNames = new( StringComparer.OrdinalIgnoreCase )
	{
		["Citizen"] = JobTag.Citizen,
		["PoliticalPrisoner"] = JobTag.PoliticalPrisoner,
		["Mayoral"] = JobTag.Mayoral,
		["Police"] = JobTag.Police,
		["Chief"] = JobTag.Chief,
		["Medic"] = JobTag.Medic,
		["Government"] = JobTag.Government,
		["Hitman"] = JobTag.Hitman,
		["Sandbox"] = JobTag.Sandbox,
		["Zombat"] = JobTag.Zombat
	};

	private static readonly Dictionary<JobTag, string> DefaultJobNames = new()
	{
		[JobTag.Citizen] = "Citizen",
		[JobTag.PoliticalPrisoner] = "Political Prisoner",
		[JobTag.Mayoral] = "Mayor",
		[JobTag.Medic] = "Medic",
		[JobTag.Chief] = "Police Chief",
		[JobTag.Police] = "Police Officer",
		[JobTag.Hitman] = "Hitman",
		[JobTag.Sandbox] = "Sandbox",
		[JobTag.Zombat] = "Zombat"
	};

	public static void Apply( string jobName, JobTag tags, bool merge = true )
	{
		if ( string.IsNullOrWhiteSpace( jobName ) )
		{
			return;
		}

		var key = Normalize( jobName );
		if ( merge && NamedTags.TryGetValue( key, out var existing ) )
		{
			NamedTags[key] = existing | tags;
			return;
		}

		NamedTags[key] = tags;
	}

	public static bool Has( GameModeJobDto? job, JobTag tag )
	{
		return (Get( job ) & tag) == tag;
	}

	public static bool HasAny( GameModeJobDto? job, JobTag tags )
	{
		return (Get( job ) & tags) != JobTag.None;
	}

	public static bool HasNamedTag( GameModeJobDto? job, string? tag )
	{
		if ( job == null || string.IsNullOrWhiteSpace( tag ) )
		{
			return false;
		}

		var normalized = Normalize( tag );
		if ( (job.JobTags ?? []).Any( existing => string.Equals( Normalize( existing ), normalized, StringComparison.Ordinal ) ) )
		{
			return true;
		}

		return NamedTags.TryGetValue( normalized, out var mapped ) && Has( job, mapped )
		       || ExplicitTagNames.TryGetValue( tag.Trim(), out mapped ) && Has( job, mapped );
	}

	public static JobTag Get( GameModeJobDto? job )
	{
		if ( job == null )
		{
			return JobTag.None;
		}

		var tags = JobTag.None;

		foreach ( var rawTag in job.JobTags ?? [] )
		{
			if ( string.IsNullOrWhiteSpace( rawTag ) )
			{
				continue;
			}

			if ( ExplicitTagNames.TryGetValue( rawTag.Trim(), out var explicitTag ) )
			{
				tags |= explicitTag;
			}
		}

		if ( tags == JobTag.None && !string.IsNullOrWhiteSpace( job.Name ) )
		{
			NamedTags.TryGetValue( Normalize( job.Name ), out tags );
		}

		var group = GameModeJobs.FindGroupById( job.GameModeJobGroupId );
		if ( group != null && string.Equals( Normalize( group.Name ), "government", StringComparison.OrdinalIgnoreCase ) )
		{
			tags |= JobTag.Government;
		}

		return tags;
	}

	public static string? GetDefaultJobName( JobTag tag )
	{
		return DefaultJobNames.GetValueOrDefault( tag );
	}

	private static string Normalize( string value )
	{
		return new string( value
			.Where( c => c != ' ' && c != '_' && c != '-' )
			.Select( char.ToLowerInvariant )
			.ToArray() );
	}
}
