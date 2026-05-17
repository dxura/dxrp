namespace Dxura.RP.Game;

public interface IGameEvents : ISceneEvent<IGameEvents>
{
	/// <summary>
	///     Called on the host when a new player joins, before NetworkSpawn is called.
	/// </summary>
	void OnPlayerJoined( Player player ) {}

	/// <summary>
	///     Called on the host when a player (re)spawns.
	/// </summary>
	void OnPlayerSpawnedHost( Player player ) {}

	/// <summary>
	///     Called on the host when a player dies.
	/// </summary>
	void OnPlayerKillHost( Player player ) {}

	/// <summary>
	///     Called when a job is changed
	/// </summary>
	void OnPlayerJobChangedHost( Player player, GameModeJobDto before, GameModeJobDto after ) {}

	/// <summary>
	///     Called when a player has fully left (after grace period if applicable).
	/// </summary>
	void OnPlayerDisconnectHost( long steamId ) {}

	/// <summary>
	///     Called on the host after the map is loaded and fitted.
	/// </summary>
	void OnMapFitted() {}

	/// <summary>
	///     Called when a weapon has been shot.
	/// </summary>
	void OnWeaponShot() {}

	/// <summary>
	///     Called when the active game mode has been updated or replaced.
	/// </summary>
	void OnGameModeUpdated( GameModeDto? before, GameModeDto? after ) {}

	/// <summary>
	///     Called once a second, an alternative for OnUpdate.
	/// </summary>
	void OnSecondlyUpdate() {}
}
