using Dxura.RP.Shared;
using System.Threading.Tasks;
namespace Dxura.RP.Game.Commands;

public class RpNameCommand : ICommand
{
	public static string Name => "rpname";
	public string Command => Name;
	public string Help => Language.GetPhrase( "command.rpname.help" );
	public Permission[] RequiredPermissions => [Permission.CommandRpName];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() || !Config.Current.Game.RpNameEnabled )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:rpname", Config.Current.Game.UtilityClearCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		// Clear RP name
		if ( args.Length < 1 )
		{
			_ = SetRpNameAsync( caller, null );
			return true;
		}

		var name = raw[(Name.Length + 1)..].Trim();
		var maxLength = Config.Current.Game.RpNameMaxLength;

		if ( name.Length < 2 || name.Length > maxLength )
		{
			caller.Error( string.Format( Language.GetPhrase( "notify.rpname.length" ), maxLength ) );
			return true;
		}

		if ( !IsValidName( name ) )
		{
			caller.Error( "#notify.rpname.invalid_characters" );
			return true;
		}

		name = GameManager.ModerateText( caller.SteamId, "rp name", name );

		_ = SetRpNameAsync( caller, name );

		return true;
	}

	private static async Task SetRpNameAsync( Player caller, string? name )
	{
		var didSucceed = await ServerApiClient.SetRpName( caller.SteamId, name );
		await GameTask.MainThread();

		if ( !caller.IsValid() )
		{
			return;
		}

		if ( !didSucceed )
		{
			caller.Error( "#notify.rpname.not_allowed" );
			return;
		}

		caller.RpName = name;

		if ( string.IsNullOrWhiteSpace( name ) )
		{
			caller.Success( "#notify.rpname.cleared" );
			return;
		}

		_ = ServerApiClient.Audit( "RpName", $"{caller.SteamName} ({caller.SteamId}) set their RP name to {name}", caller.SteamId );
		caller.Success( string.Format( Language.GetPhrase( "notify.rpname.set" ), name ) );
	}

	private static bool IsValidName( string name )
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
