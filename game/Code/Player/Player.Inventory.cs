using Dxura.RP.Game.Commands;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public sealed partial class Player
{
	private void OnStartInventory()
	{
		TitleCommand.SyncOnJoin( this );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastInventoryRefresh( Guid itemId, int expectedQuantity )
	{
		if ( !IsLocalPlayer )
		{
			return;
		}

		UI.Inventory.RequestRefresh( itemId, expectedQuantity );
	}

	/// <summary>
	///     Called by the host to persist the title choice on the owner's client.
	/// </summary>
	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	public void SaveTitlePreferenceOwner( string itemId )
	{
		TitleCommand.SaveLocally( itemId );
	}

	/// <summary>
	///     Called by the host to clear the saved title preference on the owner's client.
	/// </summary>
	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	public void ClearTitlePreferenceOwner()
	{
		TitleCommand.ClearLocally();
	}

	/// <summary>
	///     Called by the owner on join to re-apply their saved title preference.
	///     Host validates inventory ownership before setting.
	/// </summary>
	[Rpc.Host( NetFlags.Reliable )]
	public void ApplyTitlePreferenceHost( Guid itemId )
	{
		var callerId = Rpc.CallerId;
		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
			return;

		_ = ApplyTitlePreferenceAsync( player, itemId );
	}

	private static async Task ApplyTitlePreferenceAsync( Player player, Guid itemId )
	{
		var inventory = await ServerApiClient.GetPlayerInventory( player.SteamId );
		await GameTask.MainThread();

		if ( !player.IsValid() )
			return;

		var item = inventory?.FirstOrDefault( x => x.Definition.Id == itemId && x.Definition.Type == Shared.ItemType.Title );
		if ( item == null )
			return;

		player.PreferredTitle = item.Definition.GrantIdentifier ?? item.Definition.Name;
	}
}
