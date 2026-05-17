namespace Dxura.RP.Game;

public class TriggerToggleBox : Component, Component.ITriggerListener
{
	[Property]
	public GameObject? TargetGameObject { get; set; }

	[Property]
	public bool ForceFirstPerson { get; set; } = false;

	private bool _wasThirdPerson;

	protected override void OnStart()
	{
		if ( !TargetGameObject.IsValid() )
		{
			Log.Warning( $"TriggerToggleBox on {GameObject} is missing a target GameObject." );
			Destroy();
			return;
		}

		SetTargetState( false );
	}

	public void OnTriggerEnter( GameObject other )
	{
		if ( !other.Tags.Has( Constants.PlayerTag ) )
		{
			return;
		}

		var player = other.Root.GetComponent<Player>();

		if ( !player.IsValid() )
		{
			return;
		}

		// Client-side local UI/controls
		if ( !player.IsLocalPlayer )
		{
			return;
		}

		if ( ForceFirstPerson )
		{
			_wasThirdPerson = player.IsThirdPersonPreferred;
			player.IsThirdPersonPreferred = false;
			player.EnterFirstPerson();
		}

		SetTargetState( true );
	}

	public void OnTriggerExit( GameObject other )
	{
		if ( !other.Tags.Has( Constants.PlayerTag ) )
		{
			return;
		}
		var player = other.Root.GetComponent<Player>();

		if ( !player.IsValid() || !player.IsLocalPlayer )
		{
			return;
		}

		if ( ForceFirstPerson && _wasThirdPerson )
		{
			player.IsThirdPersonPreferred = true;
			player.EnterThirdPerson();
		}

		SetTargetState( false );
	}

	private void SetTargetState( bool enabled )
	{
		if ( !TargetGameObject.IsValid() )
		{
			return;
		}

		TargetGameObject.Enabled = enabled;
		TargetGameObject.LocalPosition += Vector3.Up * (enabled ? 0.001f : -0.001f); // Force re-render panel
	}
}
