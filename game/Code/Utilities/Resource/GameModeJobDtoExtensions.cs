using Dxura.RP.Game.System.Events;
using Dxura.RP.Shared;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public static class GameModeJobDtoExtensions
{
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
		return LabelResolver.ResolveText( job?.Name ) ?? string.Empty;
	}

	public static string DisplayDescription( this GameModeJobDto? job )
	{
		return LabelResolver.ResolveText( job?.Description ) ?? string.Empty;
	}

	public static string DisplayName( this GameModeJobGroupDto? group )
	{
		return LabelResolver.ResolveText( group?.Name ) ?? string.Empty;
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

	public static Model GetPrimaryModel( this Player? player )
	{
		if ( player.IsValid() && player!.Renderer.IsValid() && player.Renderer.Model.IsValid() )
		{
			return player.Renderer.Model;
		}

		return player.IsValid() && player!.Job.IsValid()
			? player.Job.GetPrimaryModel()
			: GameModeJobs.Default.GetPrimaryModel();
	}

	public static Model GetPrimaryModel( this GameModeJobDto? job )
	{
		var modelPath = ResolveModelPath( job );

		if ( IsCloudIdent( modelPath ) )
		{
			return Model.Load( "models/citizen/citizen.vmdl" );
		}

		return Model.Load( modelPath );
	}

	public static async Task<Model> GetPrimaryModelAsync( this GameModeJobDto? job )
	{
		var modelPath = ResolveModelPath( job );

		if ( IsCloudIdent( modelPath ) )
		{
			var package = await Package.Fetch( modelPath, false );
			if ( package == null )
			{
				return Model.Load( "models/citizen_human/citizen_human_male.vmdl" );
			}

			if ( !package.IsMounted() )
			{
				await package.MountAsync();
			}

			modelPath = package.GetMeta( "PrimaryAsset", "" );

			if ( string.IsNullOrWhiteSpace( modelPath ) )
			{
				return Model.Load( "models/citizen_human/citizen_human_male.vmdl" );
			}

			return await Model.LoadAsync( modelPath ) ?? Model.Load( "models/citizen_human/citizen_human_male.vmdl" );
		}

		return Model.Load( modelPath );
	}

	private static string ResolveModelPath( GameModeJobDto? job )
	{
		var modelPath = job?.Model;
		return string.IsNullOrWhiteSpace( modelPath ) ? "models/citizen_human/citizen_human_male.vmdl" : modelPath;
	}

	public static bool HasCloudModel( this GameModeJobDto? job )
	{
		var modelPath = job?.Model;
		return !string.IsNullOrWhiteSpace( modelPath ) && IsCloudIdent( modelPath );
	}

	public static bool SupportsCitizenClothing( this GameModeJobDto? job )
	{
		var modelPath = ResolveModelPath( job );
		return !IsCloudIdent( modelPath ) && IsCitizenModelPath( modelPath );
	}

	private static bool IsCloudIdent( string path )
	{
		return !path.Contains( '/' ) && path.Contains( '.' );
	}

	private static bool IsCitizenModelPath( string path )
	{
		path = path.Replace( '\\', '/' ).Trim();

		return path.StartsWith( "models/citizen/", StringComparison.OrdinalIgnoreCase )
		       || path.StartsWith( "models/citizen_human/", StringComparison.OrdinalIgnoreCase );
	}

	public static string FormatPlayerSlots( this GameModeJobDto? job, int currentPlayers )
	{
		if ( job == null )
		{
			return "0";
		}

		if ( job.MaxCount <= 0 )
		{
			return currentPlayers.ToString();
		}

		return $"{currentPlayers}/{job.MaxCount}";
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
		if ( !ignoreJobRequirements && job.PlayTime.HasValue && player.PlayTime / 60f < job.PlayTime.Value )
		{
			player.Error( "#notify.job.playtime" );
			return false;
		}

		var prerequisiteJobs = GameModeJobs.GetPrerequisiteJobs( job );
		if ( !ignoreJobRequirements && prerequisiteJobs.Length > 0 && !prerequisiteJobs.Any( p => p.Id == player.Job?.Id ) )
		{
			player.Error( "#notify.job.prerequisite" );
			return false;
		}

		return true;
	}

	public static ClothingContainer BuildClothing( this GameModeJobDto job, Player? avatarSource = null,
		bool includeJobClothing = true )
	{
		var clothing = new ClothingContainer();

		if ( job.SupportsCitizenClothing() )
		{
			var source = avatarSource ?? (Player.Local.IsValid() ? Player.Local : null);
			var avatarData = source?.Network.Owner?.GetUserData( "avatar" );
			if ( !string.IsNullOrWhiteSpace( avatarData ) )
			{
				clothing.Deserialize( avatarData );
			}

			if ( includeJobClothing )
			{
				clothing.AddRange( job.GetClothingEntries() );
			}
		}

		return clothing;
	}

	public static ClothingContainer BuildClothing( this Player player )
	{
		var clothing = new ClothingContainer();

		if ( player.Job.IsValid() && !player.Job.SupportsCitizenClothing() )
		{
			return clothing;
		}

		var avatarData = player.Network.Owner?.GetUserData( "avatar" );

		if ( !string.IsNullOrWhiteSpace( avatarData ) )
		{
			clothing.Deserialize( avatarData );
		}

		if ( player.Job.IsValid() )
		{
			clothing.AddRange( player.Job.GetClothingEntries() );
		}

		return clothing;
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
