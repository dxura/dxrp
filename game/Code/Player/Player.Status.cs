using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

public struct StatusInfo
{
	public TimeUntil? Expiry { get; set; }
	public int Stacks { get; set; }

	public StatusInfo( TimeUntil? expiry, int stacks = 1 )
	{
		Expiry = expiry;
		Stacks = stacks;
	}
}

public partial class Player
{
	[Property]
	[Feature( "Status" )]
	[Sync( SyncFlags.FromHost )]
	public NetDictionary<string, StatusInfo> Statuses { get; set; } = new();

	/// <summary>
	/// Statuses that have had their removal callback fired but may still be in the NetDictionary due to sync delay.
	/// </summary>
	private readonly HashSet<string> _removedStatuses = new();

	public int StatusCount => Statuses.Count;
	public bool HasStatus( string statusName )
	{
		return Statuses.ContainsKey( statusName );
	}
	public TimeUntil? GetStatusExpiry( string statusName )
	{
		return Statuses.TryGetValue( statusName, out var info ) ? info.Expiry : null;
	}
	public int GetStatusStacks( string statusName )
	{
		return Statuses.TryGetValue( statusName, out var info ) ? info.Stacks : 0;
	}

	//
	// Callbacks
	//

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	public void OnStatusAddedOwner( string status )
	{
		_removedStatuses.Remove( status );

		var statusInstance = Status.Current.GetCachedInstance( status );
		statusInstance?.OnAddedOwner( this );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void OnStatusAddedBroadcast( string status )
	{
		var statusInstance = Status.Current.GetCachedInstance( status );
		statusInstance?.OnAddedBroadcast( this );
	}

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	public void OnStatusRemovedOwner( string status )
	{
		_removedStatuses.Add( status );

		var statusInstance = Status.Current.GetCachedInstance( status );
		statusInstance?.OnRemovedOwner( this );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void OnStatusRemovedBroadcast( string status )
	{
		var statusInstance = Status.Current.GetCachedInstance( status );
		statusInstance?.OnRemovedBroadcast( this );
	}

	public void OnStatusesUpdateOwner()
	{
		foreach ( var status in Statuses.Keys.ToArray() )
		{
			if ( _removedStatuses.Contains( status ) )
			{
				continue;
			}

			var statusInstance = Status.Current.GetCachedInstance( status );
			statusInstance?.OnUpdateOwner( this );
		}
	}

	//
	// Utility functions
	//
	public void AddStatus( string status, float? duration = null )
	{
		Assert.True( Networking.IsHost );

		Status.Current.AddStatus( this, status, duration );
	}

	public void RemoveStatus( string status )
	{
		Assert.True( Networking.IsHost );

		Status.Current.RemoveStatus( SteamId, status );
	}
}
