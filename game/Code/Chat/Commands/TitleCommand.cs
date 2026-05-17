using Dxura.RP.Shared;
using System.Threading.Tasks;

namespace Dxura.RP.Game.Commands;

public class TitleCommand : ICommand
{
	private static string PreferenceFile => $"preferred_title_{ServerApiLink.Current?.TenantId ?? "default"}";

	public static string Name => "title";
	public string Command => Name;
	public string Help => "/title <id> | /title clear";
	public bool IsUsableWhileDead => false;
	public Permission[] RequiredPermissions => [Permission.CommandTitle];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
			return false;

		if ( args.Length < 1 )
		{
			caller.SendMessage( Help );
			return true;
		}

		if ( args[0].Equals( "clear", StringComparison.OrdinalIgnoreCase ) )
		{
			caller.PreferredTitle = null;
			caller.ClearTitlePreferenceOwner();
			caller.Inventory( Language.GetPhrase( "inventory.title.cleared" ) );
			return true;
		}

		if ( !Guid.TryParse( args[0], out var itemId ) )
		{
			caller.SendMessage( Help );
			return true;
		}

		_ = SetTitleAsync( caller, itemId );
		return true;
	}

	private static async Task SetTitleAsync( Player caller, Guid itemId )
	{
		var inventory = await ServerApiClient.GetPlayerInventory( caller.SteamId );
		await GameTask.MainThread();

		if ( !caller.IsValid() )
			return;

		var item = inventory?.FirstOrDefault( x => x.Definition.Id == itemId && x.Definition.Type == ItemType.Title );
		if ( item == null )
		{
			caller.Error( "#inventory.title.not_owned" );
			return;
		}

		var title = item.Definition.GrantIdentifier ?? item.Definition.Name;
		caller.PreferredTitle = title;
		caller.Inventory( string.Format( Language.GetPhrase( "inventory.title.set" ), ResolvePhrase( title ) ) );
		caller.SaveTitlePreferenceOwner( itemId.ToString() );
	}

	private static string ResolvePhrase( string text ) => text.StartsWith( '#' ) ? Language.GetPhrase( text[1..] ) : text;

	public static void SyncOnJoin( Player player )
	{
		if ( !FileSystem.OrganizationData.FileExists( PreferenceFile ) )
			return;

		var saved = FileSystem.OrganizationData.ReadAllText( PreferenceFile );
		if ( Guid.TryParse( saved, out var itemId ) )
		{
			player.ApplyTitlePreferenceHost( itemId );
		}
	}

	public static void SaveLocally( string itemId )
	{
		FileSystem.OrganizationData.WriteAllText( PreferenceFile, itemId );
	}

	public static void ClearLocally()
	{
		if ( FileSystem.OrganizationData.FileExists( PreferenceFile ) )
			FileSystem.OrganizationData.DeleteFile( PreferenceFile );
	}
}
