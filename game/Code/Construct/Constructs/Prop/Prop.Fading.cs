using Dxura.RP.Game.Wire;
namespace Dxura.RP.Game;

public partial class Prop : Component.IPressable, IWireComponent, IBreachable
{
	// IBreachable implementation
	bool IBreachable.CanBreach()
	{
		return FadingDoor;
	}
	bool IBreachable.CanRepair()
	{
		return Config.Current.Game.FadingDoorRepairEnabled;
	}
	bool IBreachable.IsValid()
	{
		return this.IsValid();
	}

	void IBreachable.BreachHost( Vector3 position )
	{
		ForceFade( Config.Current.Game.BreachDuration );
	}

	void IBreachable.RepairHost()
	{
		RepairBreach();
	}

	[Property]
	[FeatureEnabled( "Fading" )]
	public bool FadingDoor { get; set; }

	[Property]
	[Feature( "Fading" )]
	private bool Fade { get; set; }

	[Property]
	[Feature( "Fading" )]
	private float FadeDuration { get; set; } = 1.5f;

	[Property]
	[Feature( "Fading" )]
	public bool IsFadingBreached { get; set; }

	[Property]
	[Feature( "Fading" )]
	private bool IsReversed { get; set; }

	private bool SwitchState => FadeDuration == 0f;

	// IWireComponent implementation
	public string Name => "Prop";

	public IEnumerable<WirePort> GetInputPorts()
	{
		if ( FadingDoor )
		{
			yield return WirePort.Input( "fade", WireType.Bool );
		}
	}

	public IEnumerable<WirePort> GetOutputPorts()
	{
		if ( FadingDoor )
		{
			yield return WirePort.Output( "faded", WireType.Bool );
		}
	}

	public void OnWireInput( string inputId, WireValue value )
	{
		if ( !FadingDoor )
		{
			return;
		}

		if ( inputId == "fade" && value.Type == WireType.Bool )
		{
			var fadeValue = (bool)value.Value;
			if ( fadeValue )
			{
				FadeInternal();
			}
		}
	}

	public Vector3 GetPortPosition()
	{
		return WorldPosition;
	}

	private TimeSince? _timeSinceFade;
	private float? _breachedFadeDuration;

	private void OnUpdateFading()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		// Fading state cleanup
		if ( !FadingDoor )
		{
			if ( !Fade )
			{
				return;
			}

			Fade = false;
			BroadcastFade( Fade );
			return;
		}

		// Skip auto-repair logic for breached fading doors - BreachSystem handles this now
		if ( _breachedFadeDuration.HasValue )
		{
			return;
		}

		if ( SwitchState || !_timeSinceFade.HasValue )
		{
			return;
		}

		var durationToUse = FadeDuration;
		if ( !_timeSinceFade.HasValue || _timeSinceFade <= durationToUse )
		{
			return;
		}

		var defaultState = IsReversed;

		if ( Fade != defaultState )
		{
			Fade = defaultState;
			_timeSinceFade = null;
			BroadcastFade( Fade );
		}
		else
		{
			_timeSinceFade = null;
		}
	}

	public bool CanPress( IPressable.Event e )
	{
		return FadingDoor;
	}

	public bool Press( IPressable.Event e )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "fadingdoor:use", Config.Current.Game.FadingDoorCooldown, true ) )
		{
			return false;
		}

		if ( !HasFadingDoorPermission( Player.Local.SteamId ) )
		{
			Notify.Error( "#generic.permission" );
			return false;
		}

		FadeHost();
		return true;
	}

	private bool HasFadingDoorPermission( long checkId )
	{
		return FriendSystem.Instance.HasDoorPermission( Owner, checkId );
	}

	[Rpc.Host]
	private void FadeHost()
	{
		var caller = Rpc.Caller;
		var callerId = Rpc.CallerId;
		if ( !FadingDoor || !caller.IsHost && Cooldown.Current.CheckAndStartCooldown( $"{callerId}:fadingdoor:use", Config.Current.Game.FadingDoorCooldown ) )
		{
			return;
		}

		var callerPlayer = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !callerPlayer.IsValid() )
		{
			return;
		}

		// Server-side distance check to prevent remote fading door interaction
		var distance = callerPlayer.WorldPosition.Distance( WorldPosition );
		if ( distance > Config.Current.Game.ReachDistance )
		{
			return;
		}

		if ( !HasFadingDoorPermission( callerPlayer.SteamId ) )
		{
			return;
		}

		if ( BreachSystem.IsBreached( this ) )
		{
			callerPlayer.Error( !Config.Current.Game.FadingDoorRepairEnabled ? "#notify.prybar.repair_disabled" : "#notify.fadingdoor.breached" );
			callerPlayer.Info( "Wait " + BreachSystem.GetRemainingBreachTime( this ) + "s" );
			return;
		}

		if ( _timeSinceFade != null )
		{
			return; // Already in the process of fading
		}

		FadeInternal();
	}

	private void FadeInternal()
	{
		if ( BreachSystem.IsBreached( this ) )
		{
			return;
		}

		if ( SwitchState )
		{
			Fade = !Fade;
			_timeSinceFade = null;
		}
		else
		{
			Fade = !IsReversed;

			_timeSinceFade = 0;
		}

		_breachedFadeDuration = null;
		IsFadingBreached = false;

		BroadcastFade( Fade );
	}

	public void ForceFade( float? breachDuration = null )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( IsReversed )
		{
			Fade = false;
		}
		else
		{
			Fade = true;
		}

		_timeSinceFade = 0;
		_breachedFadeDuration = breachDuration;
		IsFadingBreached = breachDuration.HasValue;

		BroadcastFade( Fade, breachDuration != null );
	}

	public void RepairBreach()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		_breachedFadeDuration = null;
		IsFadingBreached = false;

		Fade = IsReversed;
		_timeSinceFade = null;

		BroadcastFade( Fade );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastFade( bool value, bool isBreached = false )
	{
		if ( !this.IsValid() || !GameObject.IsValid() )
		{
			return;
		}

		SetFadeInternal( value );
		IsFadingBreached = isBreached;
	}

	private void SetFadeInternal( bool value )
	{
		Fade = value;

		if ( ModelRenderer.IsValid() )
		{
			ModelRenderer.SetMaterialOverride( value ? GameManager.Instance?.FadedMaterial : _overrideMaterial, "" );
		}

		GameObject.Tags.Set( Constants.NoCollideTag, value );
		GameObject.Tags.Set( Constants.FadedTag, value );

		// Trigger wire output on host
		if ( FadingDoor && Networking.IsHost )
		{
			Wire.Wire.Current?.SetOutputValue( this, "faded", value );
		}

		// Prevent players from getting stuck in faded props
		if ( Networking.IsHost && value )
		{
			var collideGuard = GameObject.GetOrAddComponent<CollideGuard>();
			collideGuard.Immediate = true;
		}
	}

	private static bool HasFadingDoorDataChanged( PropData oldData, PropData newData )
	{
		return oldData.FadingDoor != newData.FadingDoor ||
		       oldData.FadingDoorDuration != newData.FadingDoorDuration ||
		       oldData.FadingDoorIsReversed != newData.FadingDoorIsReversed;
	}

	private void UpdateFadingDoor( bool enabled, float? fadeDuration, bool isReversed )
	{
		if ( !enabled )
		{
			FadingDoor = false;

			Wire.Wire.Current?.UnregisterComponent( this );

			return;
		}

		// Add or update fading properties
		FadingDoor = true;
		FadeDuration = fadeDuration ?? 1.5f;
		IsReversed = isReversed;
		Fade = isReversed;

		Wire.Wire.Current?.RegisterComponent( this );

		SetFadeInternal( Fade );
	}
}
