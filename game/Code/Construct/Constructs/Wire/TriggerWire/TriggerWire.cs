namespace Dxura.RP.Game.Wire;

[Title( "Trigger" )]
[Category( "Wire" )]
[Icon( "bolt" )]
public class TriggerWire() : BaseWireConstruct( ConstructType.TriggerWire ), IWireEvents
{
	private TriggerWireData _data = new();

	private GameObject? _lastHitObject;
	private GameObject? _triggerSourceObject;
	private bool _hasBeenTriggeredSinceLastWireTick;

	[Property] public LineRenderer LineRenderer { get; set; } = null!;

	[Property] public GameObject EndLaserTarget { get; set; } = null!;

	[WireOutput( "triggered" )]
	public bool Triggered { get; set; } = false;

	[WireOutput( "trigger_distance" )]
	public float TriggerDistance { get; set; } = 0f;

	[WireOutput( "trigger_count" )]
	public float TriggerCount { get; set; } = 0f;

	[WireOutput( "trigger_info" )]
	public object TriggerInfo { get; set; } = "Nothing";

	[WireInput( "reset_count" )]
	public bool ResetCount
	{
		set
		{
			if ( value )
			{
				TriggerCount = 0f;
			}
		}
		get => false; // This is just a trigger, no need to store state
	}

	public override string Name => "Trigger";

	protected override void OnStart()
	{
		base.OnStart();
		UpdateLaserVisibility();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// Run trigger check on owner
		if ( !IsOwner )
		{
			return;
		}

		CheckTriggerLine();
	}

	public override void OnOcclusionChanged( bool occlude )
	{
		base.OnOcclusionChanged( occlude );
		UpdateLaserVisibility( occlude );
	}

	private void UpdateLaserVisibility( bool? occluded = null )
	{
		// Hide the laser to save performance if we're occluded or in headless mode
		LineRenderer.Enabled = !(occluded ?? Tags.Contains( Constants.OccludeTag )) && !GameManager.IsHeadless;
	}

	private void CheckTriggerLine()
	{
		// Perform raycast to check if the line is broken
		var trace = Scene.Trace.Ray( WorldPosition + WorldRotation.Up * 2.5f, EndLaserTarget.WorldPosition )
			.Radius( 2.5f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( Constants.NoCollideTag );

		if ( _data.FilterType != TriggerFilterType.Everything )
		{
			switch ( _data.FilterType )
			{

				case TriggerFilterType.PlayerOnly:
					trace = trace.WithTag( Constants.PlayerTag );
					break;
				case TriggerFilterType.EntityOnly:
					trace = trace.WithTag( Constants.EntityTag );
					break;
				case TriggerFilterType.ConstructOnly:
					trace = trace.WithTag( Constants.ConstructTag );
					break;
				case TriggerFilterType.Everything:
				default:
					break;
			}
		}

		var traceResult = trace.Run();

		var hitObject = traceResult.Hit ? traceResult.GameObject : null;

		// Latch trigger state if something breaks the line and passes filter
		if ( hitObject != _lastHitObject )
		{
			_hasBeenTriggeredSinceLastWireTick = true;
			TriggerHost( hitObject );
		}

		_lastHitObject = hitObject;
	}

	[Rpc.Host( NetFlags.Unreliable )]
	private void TriggerHost( GameObject? hitObject )
	{
		var callerId = Rpc.CallerId;
		if ( callerId != NetworkOwner )
		{
			return;
		}

		if ( hitObject == _lastHitObject )
		{
			return;
		}

		if ( hitObject.IsValid() )
		{
			var distance = WorldPosition.Distance( hitObject.WorldPosition );
			if ( distance > _data.Range + 10f ) // Extra buffer to avoid false positives
			{
				_lastHitObject = null;
				return;
			}

			_lastHitObject = hitObject;
			_triggerSourceObject = hitObject;
			_hasBeenTriggeredSinceLastWireTick = true;
		}
		else
		{
			_lastHitObject = null;
		}
	}

	public void OnWireTick()
	{
		// Only update wire outputs during wire tick
		var wasTriggered = Triggered;
		var isCurrentlyTriggered = _lastHitObject != null;
		var hasBeenTriggered = _hasBeenTriggeredSinceLastWireTick || isCurrentlyTriggered;

		// Only increment count when transitioning from not triggered to triggered
		if ( !wasTriggered && hasBeenTriggered )
		{
			TriggerCount++;
		}

		// Use the current hit object if present, otherwise fall back to the last source
		// that triggered this tick (handles fast-pass-through case)
		GameObject? infoObject;
		if ( isCurrentlyTriggered )
		{
			infoObject = _lastHitObject;
		}
		else if ( hasBeenTriggered )
		{
			infoObject = _triggerSourceObject;
		}
		else
		{
			infoObject = null;
		}

		// Update TriggerInfo and TriggerDistance BEFORE updating Triggered so that
		// any wires reacting to the Triggered signal already see the correct values
		if ( infoObject.IsValid() )
		{
			TriggerDistance = WorldPosition.Distance( infoObject.WorldPosition );

			// When info source is default, provide info based on filter type
			if ( _data.InfoSource == TriggerInfoSource.Default )
			{
				switch ( _data.FilterType )
				{
					case TriggerFilterType.PlayerOnly:
						var player = infoObject.GetComponent<Player>();
						TriggerInfo = player.IsValid() ? player.SteamId.ToString() : "?";
						break;
					case TriggerFilterType.EntityOnly:
						var entity = infoObject.GetComponent<BaseEntity>();
						TriggerInfo = entity.IsValid() && entity.Resource.IsValid()
							? ResolvePhrase( entity.Resource.DisplayName() )
							: "?";
						break;
					case TriggerFilterType.ConstructOnly:
						var construct = infoObject.GetComponent<BaseConstruct>();
						TriggerInfo = construct.IsValid() ? construct.Type.ToString() : "?";
						break;
					case TriggerFilterType.Everything:
						var description = infoObject.GetComponent<IDescription>();
						TriggerInfo = ResolvePhrase( description?.DisplayName ) ?? infoObject.Name;
						break;
				}
			}

			//  Provide scoped info when info source is specified
			switch ( _data.InfoSource )
			{
				case TriggerInfoSource.Default:
					// Handled above
					break;
				case TriggerInfoSource.PlayerJob:
					var playerInfoJob = infoObject.GetComponent<Player>();
					if ( playerInfoJob.IsValid() )
					{
						TriggerInfo = playerInfoJob.Job.IsValid() ? playerInfoJob.Job.DisplayName() : "?";
					}
					break;
				case TriggerInfoSource.PlayerWallet:
					var playerInfoWallet = infoObject.GetComponent<Player>();
					if ( playerInfoWallet.IsValid() )
					{
						TriggerInfo = playerInfoWallet.WalletBalance;
					}
					break;
				case TriggerInfoSource.Health:
					var healthComponent = infoObject.GetComponent<HealthComponent>();
					if ( healthComponent.IsValid() )
					{
						TriggerInfo = healthComponent.Health;
					}
					break;
				case TriggerInfoSource.PlayerPocket:
					var player = infoObject.GetComponent<Player>();
					if ( PocketSystem.Instance.IsValid() && player.IsValid() )
					{
						var pocketNames = PocketSystem.Instance.ListPocketItems( player.SteamId )
							.Select( ResolvePhrase );
						TriggerInfo = string.Join( ",", pocketNames );
					}
					break;
				case TriggerInfoSource.PlayerEquipment:
					var handWeaponsPlayer = infoObject.GetComponent<Player>();
					if ( handWeaponsPlayer.IsValid() )
					{
						var equipmentNames = handWeaponsPlayer.Equipment
							.Where( e => e.IsValid() && e.CanDrop )
							.Select( eq => ResolvePhrase( eq.Resource.DisplayName() ) )
							.ToList();

						TriggerInfo = string.Join( ",", equipmentNames );
					}
					break;
			}
		}
		else
		{
			TriggerInfo = "?";
			TriggerDistance = 0f;
		}

		// Only update Triggered property if the state actually changed.
		// TriggerInfo is already up-to-date above, so wires reacting to Triggered see correct values.
		if ( hasBeenTriggered != wasTriggered )
		{
			Triggered = hasBeenTriggered;
		}

		// Reset the latch for next wire tick cycle
		_hasBeenTriggeredSinceLastWireTick = false;
	}

	private static string? ResolvePhrase( string? value )
	{
		if ( string.IsNullOrEmpty( value ) )
		{
			return value;
		}
		return value.StartsWith( '#' ) ? Language.GetPhrase( value[1..] ) : value;
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as TriggerWireData ?? new TriggerWireData();

		if ( EndLaserTarget.IsValid() )
		{
			EndLaserTarget.WorldPosition = WorldPosition + WorldRotation.Up * _data.Range;
		}
	}
}
