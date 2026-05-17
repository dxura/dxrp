using Dxura.RP.Shared;
using System.Threading.Tasks;

namespace Dxura.RP.Game.Commands;

public class ForceRpNameCommand : ICommand
{
	public const string Name = "forcerpname";

	public string Command => Name;
	public string Help => Language.GetPhrase( "command.forcerpname.help" );
	public bool IsUsableWhileDead => true;
	public float? CooldownOverride => 0f;
	public Permission[] RequiredPermissions => [Permission.CommandForceRpName];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( args.Length < 1 )
		{
			caller.SendMessage( Language.GetPhrase( "command.forcerpname.usage" ) );
			return true;
		}

		var targetPlayer = CommandHelper.ResolvePlayer( caller, args[0] );
		if ( !targetPlayer.IsValid() )
		{
			return false;
		}

		if ( !RankSystem.CanTarget( caller.SteamId, targetPlayer.SteamId ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.freeze.rank_blocked" ) );
			return true;
		}

		// Clear RP name when no name argument is given beyond the target
		string? name = args.Length >= 2 ? raw[(Name.Length + 1 + args[0].Length + 1)..].Trim() : null;

		if ( name is not null )
		{
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
		}

		_ = SetRpNameAsync( caller, targetPlayer, name );
		return true;
	}

	private static async Task SetRpNameAsync( Player caller, Player target, string? name )
	{
		var targetSteamId = target.SteamId;
		var targetSteamName = target.SteamName;

		var didSucceed = await ServerApiClient.SetRpName( targetSteamId, name );
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

		if ( target.IsValid() )
		{
			target.RpName = name;
		}

		if ( string.IsNullOrWhiteSpace( name ) )
		{
			_ = ServerApiClient.Audit( "ForceRpName", $"{caller.SteamName} ({caller.SteamId}) cleared RP name of {targetSteamName} ({targetSteamId})", caller.SteamId );
			caller.Success( string.Format( Language.GetPhrase( "notify.rpname.cleared" ) ) );
			return;
		}

		_ = ServerApiClient.Audit( "ForceRpName", $"{caller.SteamName} ({caller.SteamId}) set RP name of {targetSteamName} ({targetSteamId}) to {name}", caller.SteamId );
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
