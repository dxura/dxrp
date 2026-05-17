using Dxura.RP.Game.UI;

namespace Dxura.RP.Game;

/// <summary>
/// Base class for all random events in the game
/// </summary>
public abstract class BaseEvent
{
	/// <summary>
	/// Unique identifier for the event
	/// </summary>
	public abstract string Identifier { get; }

	/// <summary>
	/// Display name for the event
	/// </summary>
	public abstract string Name { get; }

	/// <summary>
	/// Description of what the event does
	/// </summary>
	public abstract string Description { get; }

	/// <summary>
	/// Duration of the event in seconds, or 0 for instant events
	/// </summary>
	public abstract float Duration { get; }

	/// <summary>
	/// Weight for random selection (higher = more likely to be chosen)
	/// </summary>
	public virtual int Weight => 100;

	/// <summary>
	/// Whether this event is currently active
	/// </summary>
	public bool IsActive { get; private set; }

	/// <summary>
	/// When the event will end (if it has a duration)
	/// </summary>
	protected float EndTime { get; private set; }

	/// <summary>
	/// Start the event
	/// </summary>
	public void Start()
	{
		if ( IsActive )
		{
			return;
		}

		IsActive = true;

		if ( Duration > 0 )
		{
			EndTime = Time.Now + Duration;
		}

		NotifyAllPlayers();
		OnStart();

		if ( Duration > 0 )
		{
			Log.Info( $"Event duration: {Duration}" );

			// Schedule end if this is a timed event
			GameTask.RunInThreadAsync( async () =>
			{
				await GameTask.DelayRealtimeSeconds( Duration );
				await GameTask.MainThread();

				End();
			} );
		}
		else
		{
			// For instant events, end immediately after starting
			IsActive = false;
		}
	}

	/// <summary>
	/// End the event
	/// </summary>
	public void End()
	{
		if ( !IsActive )
		{
			return;
		}

		IsActive = false;
		OnEnd();

		if ( Duration > 0 )
		{
			NotifyEventEnded();
		}
	}

	/// <summary>
	/// Called when the event starts
	/// </summary>
	protected abstract void OnStart();

	/// <summary>
	/// Called when the event ends (for timed events)
	/// </summary>
	protected virtual void OnEnd() {}

	/// <summary>
	/// Called once per second while the event is active
	/// </summary>
	public virtual void OnSecondlyUpdate() {}

	/// <summary>
	/// Check if this event can be triggered in the current game state
	/// </summary>
	public virtual bool CanTrigger()
	{
		return GameUtils.Players.Any();
	}

	/// <summary>
	/// Send a notification to all players about the event starting
	/// </summary>
	protected void NotifyAllPlayers()
	{
		Chat.Current?.BroadcastSystemText( $"[Event] {Name} - {Description}" );
		Log.Info( $"Event started: {Name}" );
	}

	/// <summary>
	/// Send a notification to all players about the event ending
	/// </summary>
	protected void NotifyEventEnded()
	{
		Chat.Current?.BroadcastSystemText( $"[Event] {Name} Ended" );
		Log.Info( $"Event ended: {Name}" );
	}
}
