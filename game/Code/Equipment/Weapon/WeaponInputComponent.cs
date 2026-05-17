namespace Dxura.RP.Game;

/// <summary>
///     A weapon component that reacts to input actions.
/// </summary>
public abstract class InputWeaponComponent : WeaponComponent,
	IEquipmentEvents
{
	/// <summary>
	///     What input action are we going to listen for?
	/// </summary>
	[Property]
	[Category( "Base" )]
	public List<string> InputActions { get; set; } = new()
	{
		"Attack1"
	};

	/// <summary>
	///     Should we perform the action when ALL input actions match, or any?
	/// </summary>
	[Property]
	[Category( "Base" )]
	public bool RequiresAllInputActions { get; set; }

	private bool _isDown;

	public void OnEquipmentDeployed( Equipment equipment )
	{
	}

	public void OnEquipmentHolstered( Equipment equipment )
	{
		if ( Equipment.Owner?.IsLocalPlayer == true )
		{
			OnHolstered();
		}
	}

	protected bool IsDown()
	{
		return _isDown;
	}

	/// <summary>
	///     Called when the input method succeeds.
	/// </summary>
	protected virtual void OnInput()
	{
	}

	/// <summary>
	///     When the button is up
	/// </summary>
	protected virtual void OnInputUp()
	{
	}

	/// <summary>
	///     When the button is down
	/// </summary>
	protected virtual void OnInputDown()
	{
	}

	/// <summary>
	///     Called every frame when input is being processed
	/// </summary>
	protected virtual void OnInputUpdate()
	{
	}

	protected virtual void OnHolstered() {}

	protected override void OnUpdate()
	{
		// Process input in Update for better responsiveness
		ProcessInput();
	}

	protected override void OnFixedUpdate()
	{
		// Only handle physics-related operations in FixedUpdate
		if ( ShouldProcess() )
		{
			OnInputFixedUpdate();
		}
	}

	/// <summary>
	///     For physics-related input handling that needs fixed timestep
	/// </summary>
	protected virtual void OnInputFixedUpdate()
	{
	}

	private bool ShouldProcess()
	{
		if ( !Equipment.IsValid() || !Equipment.IsDeployed || !Equipment.Owner.IsValid() )
		{
			return false;
		}

		// We only care about input actions coming from the owning object.
		if ( !Equipment.Owner.IsLocalPlayer )
		{
			return false;
		}

		return true;
	}

	protected SceneTraceResult? GetTrace( float maxRange = 750f, float size = 1f )
	{
		var aimRay = Equipment.Owner?.AimRay;
		if ( !aimRay.HasValue )
		{
			return null;
		}

		var start = aimRay.Value.Position;
		var end = aimRay.Value.Position + aimRay.Value.Forward * maxRange;

		return Scene.Trace.Ray( start, end )
			.UseHitboxes()
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.WithoutTags( Constants.TraceIgnoreTags )
			.Size( size )
			.Run();
	}

	private void ProcessInput()
	{
		if ( !ShouldProcess() )
		{
			return;
		}

		// Call update callback
		OnInputUpdate();

		// Process input state changes
		var matched = CheckInputMatched();

		if ( matched )
		{
			OnInput();

			if ( !_isDown )
			{
				OnInputDown();
				_isDown = true;
			}
		}
		else if ( _isDown )
		{
			OnInputUp();
			_isDown = false;
		}
	}

	private bool CheckInputMatched()
	{
		if ( RequiresAllInputActions )
		{
			return InputActions.All( action => Input.Down( action ) );
		}
		else
		{
			return InputActions.Any( action => Input.Down( action ) );
		}
	}
}
