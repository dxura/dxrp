namespace Dxura.RP.Game.Commands;

public class CustomJobCommand : ICommand
{
	public static string Name => "customjob";
	public string Command => Name;
	public string Help => "/customjob <job_name>";

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() || !Config.Current.Game.CustomJobEnabled )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:customJob", Config.Current.Game.ChangeCustomJobCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		if ( !caller.Job.IsCitizenRole() )
		{
			caller.Error( "#notify.customjob.citizen" );
			return true;
		}

		if ( args.Length < 1 )
		{
			return false;
		}

		var name = raw[(Name.Length + 1)..].Trim();

		if ( name.Length is < 2 or > 15 )
		{
			caller.Error( "#notify.customjob.invalid" );
			return false;
		}

		if ( !IsValidJobName( name ) )
		{
			caller.Error( "#notify.customjob.invalid_characters" );
			return false;
		}

		if ( caller.CustomJob == name )
		{
			return true;
		}

		name = GameManager.ModerateText( caller.SteamId, "custom job", name );

		caller.CustomJob = name;
		caller.Success( "#notify.customjob.set" );

		_ = ServerApiClient.Audit( "CustomJob", $"{caller.SteamName} ({caller.SteamId}) set their custom job to {name}", caller.SteamId );

		return true;
	}

	private static bool IsValidJobName( string name )
	{
		foreach ( var c in name )
		{
			if ( !char.IsLetterOrDigit( c ) && c != ' ' )
			{
				return false;
			}
		}
		return true;
	}
}
