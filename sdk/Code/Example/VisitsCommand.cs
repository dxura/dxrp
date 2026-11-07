using System.Threading.Tasks;

namespace Dxura.RP.Game.Addons.Example;

/// <summary>
/// /visits: a host-handled chat command that keeps a per-player counter in the Store.
/// Found through TypeLibrary, so there's nothing to register.
/// </summary>
public sealed class VisitsCommand : ICommand
{
	// Prefix Store keys with the addon identifier so they can't collide with another addon's
	private const string Prefix = "example:visits:";

	public string Command => "visits";
	public string Help => "Shows how many times you have used /visits";

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		_ = Increment( caller );
		return true;
	}

	private static async Task Increment( Player caller )
	{
		var key = Prefix + caller.SteamId;
		var data = await ServerApiClient.GetStoreJson<VisitData>( key ) ?? new VisitData();

		data.Count++;
		await ServerApiClient.SetStoreJson( key, data );

		// Backend calls can resume off the game thread, and the player may have left meanwhile
		await GameTask.MainThread();
		if ( caller.IsValid() )
		{
			caller.SendMessage( $"Visit count: {data.Count}" );
		}
	}

	public sealed class VisitData
	{
		public int Count { get; set; }
	}
}
