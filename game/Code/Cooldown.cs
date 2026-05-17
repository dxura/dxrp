namespace Dxura.RP.Game;

/// <summary>
///     Manages cooldown timers for game actions.
/// </summary>
public class Cooldown : GameObjectSystem<Cooldown>
{
	public event Action<string>? CooldownStarted;
	public event Action<string>? CooldownCompleted;

	private readonly Dictionary<string, TimeUntil> _activeCooldowns = new();

	public Cooldown( Scene scene ) : base( scene )
	{
		Listen( Stage.FinishUpdate, 10, ProcessCooldowns, "ProcessCooldowns" );
	}

	/// <summary>
	///     Checks for completed cooldowns.
	/// </summary>
	private void ProcessCooldowns()
	{
		foreach ( var (cooldownId, timeRemaining) in _activeCooldowns.ToList() )
		{
			if ( timeRemaining )
			{
				_activeCooldowns.Remove( cooldownId );
				CooldownCompleted?.Invoke( cooldownId );
			}
		}
	}

	/// <summary>
	///     Checks if a cooldown is currently active.
	/// </summary>
	/// <param name="cooldownId">The unique identifier for the cooldown.</param>
	/// <returns>True if the cooldown is active, false otherwise.</returns>
	public bool IsOnCooldown( string cooldownId )
	{
		if ( !_activeCooldowns.TryGetValue( cooldownId, out var timeRemaining ) )
		{
			return false;
		}

		return !timeRemaining;
	}

	/// <summary>
	///     Starts a cooldown with the specified duration.
	/// </summary>
	/// <param name="cooldownId">The unique identifier for the cooldown.</param>
	/// <param name="duration">The duration in seconds.</param>
	public void StartCooldown( string cooldownId, float duration )
	{
		_activeCooldowns[cooldownId] = duration;
		CooldownStarted?.Invoke( cooldownId );
	}

	/// <summary>
	///     Cancels an active cooldown.
	/// </summary>
	/// <param name="cooldownId">The unique identifier for the cooldown.</param>
	public void CancelCooldown( string cooldownId )
	{
		_activeCooldowns.Remove( cooldownId );
	}

	/// <summary>
	///     Get remaining cooldown time in seconds.
	/// </summary>
	/// <param name="cooldownId">The unique identifier for the cooldown.</param>
	/// <returns>The remaining time in seconds, or 0 if not on cooldown.</returns>
	public int GetRemainingTime( string cooldownId )
	{
		if ( !_activeCooldowns.TryGetValue( cooldownId, out var timeRemaining ) )
		{
			return 0;
		}

		return (int)timeRemaining;
	}

	/// <summary>
	///     Checks if a cooldown is active and starts it if not.
	/// </summary>
	/// <param name="cooldownId">The unique identifier for the cooldown.</param>
	/// <param name="duration">The duration in seconds.</param>
	/// <param name="notify">If the user should be notified for this cooldown</param>
	/// <returns>True if the cooldown was already active, false if it was just started.</returns>
	/// <example>
	///     if (CooldownManager.Instance.CheckAndStartCooldown("ability_fireball", 5.0f))
	///     return; // Cooldown is active, don't proceed
	/// </example>
	public bool CheckAndStartCooldown( string cooldownId, float duration, bool notify = false )
	{
		if ( IsOnCooldown( cooldownId ) )
		{
			if ( notify )
			{
				Notify.Cooldown( cooldownId );
			}

			return true;
		}

		StartCooldown( cooldownId, duration );
		return false;
	}
}
