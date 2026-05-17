using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

public partial class Status
{
	public void AddStatus( long steamId, string status, float? duration = null )
	{
		var player = GameUtils.GetPlayerById( steamId );
		if ( !player.IsValid() )
		{
			return;
		}

		AddStatus( player, status, duration );
	}

	public void AddStatus( Player player, string status, float? duration = null )
	{
		Assert.True( Networking.IsHost );

		if ( !_statusTypes.TryGetValue( status, out var typeDescription ) )
		{
			Log.Warning( $"Status type '{status}' not found" );
			return;
		}

		if ( TypeLibrary.Create<IStatus>( typeDescription.TargetType ) is not {} statusInstance )
		{
			Log.Warning( $"Failed to create instance of status type '{status}'" );
			return;
		}

		if ( !_activeStatuses.TryGetValue( player.SteamId, out var statuses ) )
		{
			statuses = [];
			_activeStatuses[player.SteamId] = statuses;
		}

		var effectiveDuration = duration ?? statusInstance.DefaultDuration;

		if ( statuses.Any( s => s.Id == status ) )
		{
			// Status already active, handle stacking and duration refresh
			var existingStatus = statuses.First( s => s.Id == status );

			// Reset the timer properly
			existingStatus.Expiry = effectiveDuration;

			// Handle stacking if applicable
			if ( existingStatus.Stackable && existingStatus.CurrentStacks < existingStatus.MaxStacks )
			{
				existingStatus.CurrentStacks++;
			}

			// Update the player's status dictionary with new expiry and stack count
			player.Statuses[status] = new StatusInfo( existingStatus.Expiry, existingStatus.CurrentStacks );

			return;
		}

		statusInstance.Expiry = effectiveDuration;

		statuses.Add( statusInstance );
		player.Statuses[status] = new StatusInfo( statusInstance.Expiry, statusInstance.CurrentStacks );

		statusInstance.OnAddedServer( player );
		player.OnStatusAddedOwner( status );
		player.OnStatusAddedBroadcast( status );
	}

	public void RemoveStatus( long steamId, string status )
	{
		Assert.True( Networking.IsHost );

		var player = GameUtils.GetPlayerById( steamId );
		if ( !player.IsValid() )
		{
			return;
		}

		RemoveStatus( player, status );
	}

	public void RemoveStatus( Player player, string status )
	{
		if ( !_activeStatuses.TryGetValue( player.SteamId, out var statuses ) )
		{
			return;
		}

		var statusInstance = statuses.FirstOrDefault( s => s.Id == status );
		if ( statusInstance == null )
		{
			return;
		}

		statusInstance.OnRemovedServer( player );
		statuses.Remove( statusInstance );
		player.Statuses.Remove( status );

		player.OnStatusRemovedOwner( status );
		player.OnStatusRemovedBroadcast( status );
	}
}
