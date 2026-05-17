namespace Dxura.RP.Game.System.Events;

/// <summary>
/// Manages and schedules random events in the game
/// </summary>
public class EventSystem : SingletonComponent<EventSystem>, IGameEvents
{
	private const float EventCheckInterval = 60f;

	private float _nextEventTime;
	private float _nextCheckTime;
	private readonly Dictionary<string, BaseEvent> _registeredEvents = new();
	private readonly List<BaseEvent> _activeEvents = new();
	private readonly Random _random = new();

	protected override void OnAwake()
	{
		base.OnAwake();

		// Register all event types
		RegisterEvents();

		if ( !Config.Current.Game.EventsEnabled || !Networking.IsHost )
		{
			return;
		}

		// Schedule first event
		ScheduleNextEvent();
	}

	public void OnSecondlyUpdate()
	{
		if ( !Config.Current.Game.EventsEnabled || !Networking.IsHost )
		{
			return;
		}

		var currentTime = Time.Now;

		// Update all active events
		for ( var i = _activeEvents.Count - 1; i >= 0; i-- )
		{
			var activeEvent = _activeEvents[i];

			if ( !activeEvent.IsActive )
			{
				_activeEvents.RemoveAt( i );
				continue;
			}

			activeEvent.OnSecondlyUpdate();
		}

		// Check if it's time for a new event
		if ( currentTime >= _nextCheckTime )
		{
			_nextCheckTime = currentTime + EventCheckInterval;

			if ( currentTime >= _nextEventTime )
			{
				TriggerRandomEvent();
				ScheduleNextEvent();
			}
		}
	}

	/// <summary>
	/// Register all available event types
	/// </summary>
	private void RegisterEvents()
	{
		// Clear existing events
		_registeredEvents.Clear();

		// This is where we'll register all event types in the system
		// Each event will be instantiated and added to the registry

		// We'll discover and register events automatically using reflection
		RegisterEventsByReflection();

		Log.Info( $"Registered {_registeredEvents.Count} random events" );
	}

	/// <summary>
	/// Register events by finding all classes that derive from RandomEvent
	/// </summary>
	private void RegisterEventsByReflection()
	{
		var eventTypes = TypeLibrary.GetTypes<BaseEvent>();

		foreach ( var type in eventTypes.Where( x => !x.IsAbstract ) )
		{
			try
			{
				var instance = TypeLibrary.Create<BaseEvent>( type.TargetType );
				if ( instance == null )
				{
					continue;
				}

				_registeredEvents[instance.Identifier] = instance;
				Log.Info( $"Registered event: {instance.Name} ({instance.Identifier})" );
			}
			catch ( Exception ex )
			{
				Log.Error( $"Failed to register event type {type.Name}: {ex.Message}" );
			}
		}
	}

	/// <summary>
	/// Schedule when the next event should trigger
	/// </summary>
	private void ScheduleNextEvent()
	{
		// Calculate a random time for the next event
		var interval = _random.Float( Config.Current.Game.MinTimeBetweenEvents, Config.Current.Game.MaxTimeBetweenEvents );
		_nextEventTime = Time.Now + interval;

		Log.Info( $"Next random event scheduled in {interval / 60:F1} minutes" );
	}

	/// <summary>
	/// Trigger a random event from the registered events
	/// </summary>
	private void TriggerRandomEvent()
	{
		if ( _registeredEvents.Count == 0 )
		{
			Log.Warning( "No events registered, can't trigger a random event" );
			return;
		}

		// Get all events that can be triggered right now
		var eligibleEvents = _registeredEvents.Values
			.Where( e => e.CanTrigger() && !e.IsActive )
			.ToList();

		if ( eligibleEvents.Count == 0 )
		{
			Log.Info( "No eligible events to trigger right now" );
			return;
		}

		// Weight-based random selection
		var totalWeight = eligibleEvents.Sum( e => e.Weight );
		var randomValue = _random.Int( 0, totalWeight - 1 );

		var accumulatedWeight = 0;
		foreach ( var evt in eligibleEvents )
		{
			accumulatedWeight += evt.Weight;
			if ( randomValue >= accumulatedWeight )
			{
				continue;
			}

			StartEvent( evt );
			break;
		}
	}

	/// <summary>
	/// Start a specific event
	/// </summary>
	private void StartEvent( BaseEvent randomEvent )
	{
		if ( !randomEvent.IsActive )
		{
			randomEvent.Start();
			_activeEvents.Add( randomEvent );

			Log.Info( $"Started event: {randomEvent.Name}" );
		}
	}

	/// <summary>
	/// Start a specific event by identifier
	/// </summary>
	private void StartEvent( string eventIdentifier )
	{
		if ( _registeredEvents.TryGetValue( eventIdentifier, out var randomEvent ) )
		{
			StartEvent( randomEvent );
		}
		else
		{
			Log.Warning( $"Tried to start unknown event: {eventIdentifier}" );
		}
	}

	/// <summary>
	/// Check if a specific event is currently active
	/// </summary>
	public bool IsEventActive( string eventIdentifier )
	{
		return _activeEvents.Any( e => e.Identifier == eventIdentifier );
	}

	/// <summary>
	/// Start a specific event by identifier
	/// </summary>
	public void Toggle( string eventIdentifier )
	{
		var currentEvent = _activeEvents.FirstOrDefault( e => e.Identifier == eventIdentifier );

		if ( currentEvent != null )
		{
			_activeEvents.Remove( currentEvent );
			currentEvent.End();

			Log.Info( $"Stopped event: {eventIdentifier}" );
		}
		else
		{
			StartEvent( eventIdentifier );
			Log.Info( $"Started event: {eventIdentifier}" );

		}
	}
}
