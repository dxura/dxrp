namespace Dxura.RP.Game;

public partial class Status : IGameEvents
{
	public void OnPlayerSpawnedHost( Player player )
	{
		if ( !player.IsValid() || !_activeStatuses.TryGetValue( player.SteamId, out var playerStatuses ) )
		{
			return;
		}

		var statusesToRemove = (from status in playerStatuses where status.RemoveOnRespawn select status.Id).ToList();

		foreach ( var statusName in statusesToRemove )
		{
			RemoveStatus( player, statusName );
		}
	}

	public void OnPlayerJobChangedHost( Player player, GameModeJobDto before, GameModeJobDto after )
	{
		if ( !player.IsValid() || !_activeStatuses.TryGetValue( player.SteamId, out var playerStatuses ) )
		{
			return;
		}

		var statusesToRemove = (from status in playerStatuses where status.RemoveOnJobChange select status.Id).ToList();

		foreach ( var statusName in statusesToRemove )
		{
			RemoveStatus( player, statusName );
		}
	}

	public void OnPlayerKillHost( Player player )
	{
		if ( !player.IsValid() || !_activeStatuses.TryGetValue( player.SteamId, out var playerStatuses ) )
		{
			return;
		}

		var statusesToRemove = (from status in playerStatuses where status.RemoveOnDeath select status.Id).ToList();

		foreach ( var statusName in statusesToRemove )
		{
			RemoveStatus( player, statusName );
		}
	}

	// Cleanup statuses on disconnect
	public void OnPlayerDisconnectHost( long steamId )
	{
		_activeStatuses.Remove( steamId );
	}

}
