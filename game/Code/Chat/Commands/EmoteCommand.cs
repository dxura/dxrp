using Dxura.RP.Shared;
using System.Threading.Tasks;

namespace Dxura.RP.Game.Commands;

public class EmoteCommand : ICommand
{
	public string Command => "emote";
	public string[] Aliases => ["e"];
	public string Help => "/emote <identifier> - Play an emote, or /emote to stop";
	public bool IsUsableWhileDead => false;
	public bool IsUsableWhileFrozen => false;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		// No args = stop current emote
		if ( args.Length == 0 )
		{
			if ( caller.CurrentEmote.IsValid() )
			{
				caller.StopEmoteHost();
				return true;
			}

			return false;
		}

		var grantIdentifier = args[0];
		_ = PlayGrantedEmoteAsync( caller, grantIdentifier );
		return true;
	}

	public static async Task PlayGrantedEmoteAsync( Player caller, string grantIdentifier )
	{
		var emote = EmoteResource.All
			.FirstOrDefault( e => e.SequenceName.Equals( grantIdentifier, StringComparison.OrdinalIgnoreCase ) );

		await GameTask.MainThread();
		if ( !caller.IsValid() )
		{
			return;
		}
		
		if ( emote == null )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.emote.unknown" ), grantIdentifier ) );
			return;
		}

		var inventory = await ServerApiClient.GetPlayerInventory( caller.SteamId );
		await GameTask.MainThread();

		if ( !caller.IsValid() )
		{
			return;
		}

		if ( !HasEmoteGrant( inventory, grantIdentifier ) )
		{
			caller.Error( "You do not own this emote." );
			return;
		}

		caller.PlayEmoteHost( emote );
	}

	private static bool HasEmoteGrant( IReadOnlyList<InventoryItemDto>? inventory, string grantIdentifier )
	{
		if ( !ServerApiLink.HasAuthorizationKey ) return true;
		
		return inventory?.Any( item =>
			item.Quantity > 0 &&
			item.Definition.Type == ItemType.Emote &&
			string.Equals( item.Definition.GrantIdentifier, grantIdentifier, StringComparison.OrdinalIgnoreCase ) ) == true;
	}
}
