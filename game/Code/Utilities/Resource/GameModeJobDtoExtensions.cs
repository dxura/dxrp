using Dxura.RP.Game.System.Events;
using Dxura.RP.Shared;

namespace Dxura.RP.Game;

public static class GameModeJobDtoExtensions
{
	private static readonly Dictionary<string, Model> ModelCache = new( StringComparer.OrdinalIgnoreCase );

	public static Color ColorValue( this GameModeJobDto? job )
	{
		return (job?.Color ?? 0).ToColor();
	}

	public static Color ColorValue( this GameModeJobGroupDto? group )
	{
		return (group?.Color ?? 0).ToColor();
	}

	public static string DisplayName( this GameModeJobDto? job )
	{
		return job?.Name ?? string.Empty;
	}

	public static string DisplayDescription( this GameModeJobDto? job )
	{
		return job?.Description ?? string.Empty;
	}

	public static string DisplayName( this GameModeJobGroupDto? group )
	{
		return group?.Name ?? string.Empty;
	}

	public static GameModeJobGroupDto? GetGroup( this GameModeJobDto? job )
	{
		return GameModeJobs.FindGroupById( job?.GameModeJobGroupId );
	}

	public static bool IsInGroup( this GameModeJobDto? job, string groupIdentifier )
	{
		var group = job.GetGroup();
		return group != null && string.Equals( group.Name, groupIdentifier, StringComparison.OrdinalIgnoreCase );
	}

	public static Model GetPrimaryModel( this GameModeJobDto? job )
	{
		var modelPath = job?.Model;
		if ( string.IsNullOrWhiteSpace( modelPath ) )
		{
			modelPath = GameModeJobs.Default.Model;
		}

		if ( string.IsNullOrWhiteSpace( modelPath ) )
		{
			modelPath = "models/citizen/citizen.vmdl";
		}

		if ( ModelCache.TryGetValue( modelPath, out var cached ) )
		{
			return cached;
		}

		// Try loading (might be cloud)
		Cloud.Load(  modelPath );
		
		cached = Model.Load( modelPath );
		ModelCache[modelPath] = cached;
		return cached;
	}

	public static bool HasTag( this GameModeJobDto? job, JobTag tag )
	{
		return JobTags.Has( job, tag );
	}

	public static bool HasAnyTag( this GameModeJobDto? job, JobTag tags )
	{
		return JobTags.HasAny( job, tags );
	}

	public static bool IsValid( this GameModeJobDto? job )
	{
		return job != null && job.Id != Guid.Empty && !string.IsNullOrWhiteSpace( job.Name );
	}

	public static bool IsGovernmentRole( this GameModeJobDto? job )
	{
		return job.HasTag( JobTag.Government );
	}

	public static bool IsMayoralRole( this GameModeJobDto? job )
	{
		return job.HasTag( JobTag.Mayoral );
	}

	public static bool IsChiefRole( this GameModeJobDto? job )
	{
		return job.HasTag( JobTag.Chief );
	}

	public static bool IsPoliceRole( this GameModeJobDto job )
	{
		return job.HasTag( JobTag.Police );
	}

	public static bool IsMedicRole( this GameModeJobDto? job )
	{
		return job.HasTag( JobTag.Medic );
	}

	public static bool IsCitizenRole( this GameModeJobDto? job )
	{
		return job.HasTag( JobTag.Citizen );
	}

	public static bool IsPoliticalPrisonerRole( this GameModeJobDto? job )
	{
		return job.HasTag( JobTag.PoliticalPrisoner );
	}

	public static bool IsHitmanRole( this GameModeJobDto? job )
	{
		return job.HasTag( JobTag.Hitman );
	}

	public static bool AssignableTo( this GameModeJobDto job, Player player )
	{
		if ( player.Job == job )
		{
			return false;
		}

		if ( !job.Selectable )
		{
			return false;
		}

		if ( player.Restricted )
		{
			player.Error( "#notify.job.prisoner" );
			return false;
		}

		if ( job.MaxCount != 0 && GameUtils.GetPlayersByJob( job ).Count() >= job.MaxCount )
		{
			player.Error( "#notify.job.full" );
			return false;
		}

		var ignoreJobRequirements = GameManager.Instance.IsValid() && GameManager.Instance.IgnoreJobRequirements;
		if ( !ignoreJobRequirements && job.PlayTime.HasValue && player.PlayTime < job.PlayTime.Value )
		{
			player.Error( "#notify.job.playtime" );
			return false;
		}

		var prerequisiteJobs = GameModeJobs.GetPrerequisiteJobs( job );
		if ( !ignoreJobRequirements && prerequisiteJobs.Length > 0 && !prerequisiteJobs.Contains( player.Job ) )
		{
			player.Error( "#notify.job.prerequisite" );
			return false;
		}

		return true;
	}

	public static IEnumerable<ClothingContainer.ClothingEntry> GetClothingEntries( this GameModeJobDto job )
	{
		var container = new ClothingContainer();

		foreach ( var clothingPath in job.Clothes )
		{
			if ( string.IsNullOrWhiteSpace( clothingPath ) )
			{
				continue;
			}

			var clothing = ResourceLibrary.Get<Clothing>( clothingPath.Trim() );
			if ( clothing == null )
			{
				continue;
			}

			container.Add( clothing );
		}

		foreach ( var entry in container.Clothing )
		{
			yield return entry;
		}
	}
}
