namespace Dxura.RP.Game;

public interface IStatus
{
	// Identity
	string Id { get; }
	string Name { get; }
	string LongName { get; }
	string? Icon { get; }
	string? MaterialIcon { get; }
	Color Color { get; }

	// Tracking
	TimeUntil? Expiry { get; set; }
	bool IsExpired => Expiry <= 0;

	// Duration
	float? DefaultDuration { get; }
	bool RemoveOnDeath { get; }
	bool RemoveOnJobChange { get; }
	bool RemoveOnRespawn { get; }
	bool RemoveOnArrest { get; }

	// Display
	bool Visible { get; }
	bool ShowText { get; }
	bool ShowOnNameplate { get; }
	bool ShowOnPlayerList { get; }

	// Stacks
	bool Stackable { get; }
	int CurrentStacks { get; set; }
	int MaxStacks { get; }

	// Modifiers
	bool PreventFallDamage => false;

	// Lifecycle

	/// <summary>
	/// Called on the server when the status is added.
	/// </summary>
	/// <param name="player">The player this is for</param>
	void OnAddedServer( Player player ) {}
	/// <summary>
	/// Called on the player's client instance when the status is added.
	/// </summary>
	/// <remarks>This is stateless</remarks>
	/// <param name="player">The player this is for</param>
	void OnAddedOwner( Player player ) {}
	/// <summary>
	/// Called on ALL clients when the status becomes active (when added or when a client joins).
	/// Use this for visual effects that all players need to see (e.g., invisibility, glows, particles).
	/// </summary>
	/// <remarks>This is stateless and called on all clients</remarks>
	/// <param name="player">The player this is for</param>
	void OnAddedBroadcast( Player player ) {}

	/// <summary>
	/// Called on the server when the status is removed.
	/// </summary>
	/// <param name="player">The player this is for</param>
	void OnRemovedServer( Player player ) {}
	/// <summary>
	/// Called on the player's client instance when the status is removed.
	/// </summary>
	/// <remarks>This is stateless</remarks>
	/// <param name="player">The player this is for</param>
	void OnRemovedOwner( Player player ) {}
	/// <summary>
	/// Called on ALL clients when the status becomes inactive (removed).
	/// Use this to clean up visual effects.
	/// </summary>
	/// <remarks>This is stateless and called on all clients</remarks>
	/// <param name="player">The player this is for</param>
	void OnRemovedBroadcast( Player player ) {}

	/// <summary>
	/// Called once per second on server while the status is active.
	/// </summary>
	/// <param name="player">The player this is for</param>
	void OnSecondlyUpdateServer( Player player ) {}
	/// <summary>
	/// Called every frame on the player's client instance while the status is active.
	/// </summary>
	/// <remarks>This is stateless</remarks>
	/// <param name="player">The player this is for</param>
	void OnUpdateOwner( Player player ) {}

	/// <summary>
	/// Called on the server to modify outgoing chat messages.
	/// </summary>
	/// <param name="player">The player sending the message</param>
	/// <param name="message">The message text</param>
	/// <param name="messageType">The chat channel</param>
	/// <returns>The modified message</returns>
	string ModifyChat( Player player, string message, MessageType messageType ) => message;

	/// <summary>
	/// Called on the server to modify incoming damage.
	/// </summary>
	/// <returns>The modified damage multiplier (1.0 = no change)</returns>
	float ModifyDamageTaken( Player player ) => 1f;
}
