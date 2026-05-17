namespace Dxura.RP.Game;

public class DeadBody : Component, IDescription
{
	/// <summary>
	///     A reference to the dead player's <see cref="Player" />.
	/// </summary>
	[Property] [ReadOnly]
	[Sync( SyncFlags.FromHost )]
	public Player Player { get; set; } = Player.Local;

	private readonly Dictionary<Player, Rigidbody> _currentDraggers = new();
	private float MovementSmoothness => 8f;
	private const float GrabDistance = 65;

	public string DisplayName => Player.IsValid() ? $"{Player.DisplayName} (Dead)" : "Corpse";

	private float CameraHeight { get; set; } = 80f;
	private float CameraDistance { get; set; } = 150f;
	private float OrbitSpeed { get; set; } = 0.3f;
	private float SmoothSpeed { get; set; } = 2f;

	private float _orbitAngle;

	protected override void OnStart()
	{
		if ( GameManager.IsHeadless )
		{
			return;
		}

		DressFromPlayer();
	}

	private void DressFromPlayer()
	{
		if ( !Player.IsValid() )
		{
			return;
		}

		var skinnedModelRenderer1 = Player.Renderer;
		if ( !skinnedModelRenderer1.IsValid() )
		{
			return;
		}

		var skinnedModelRenderer2 = GetOrAddComponent<SkinnedModelRenderer>();
		skinnedModelRenderer2.CopyFrom( skinnedModelRenderer1 );
		skinnedModelRenderer2.UseAnimGraph = false;
		foreach ( var other in skinnedModelRenderer1.GameObject.Children.SelectMany<GameObject, SkinnedModelRenderer>( (Func<GameObject, IEnumerable<SkinnedModelRenderer>>)(x => x.Components.GetAll<SkinnedModelRenderer>()) ) )
		{
			if ( other.IsValid() )
			{
				var skinnedModelRenderer3 = new GameObject( true, other.GameObject.Name )
				{
					Parent = GameObject
				}.Components.Create<SkinnedModelRenderer>();
				skinnedModelRenderer3.CopyFrom( other );
				skinnedModelRenderer3.BoneMergeTarget = skinnedModelRenderer2;
			}
		}
	}

	protected override void OnUpdate()
	{
		if ( !Player.IsValid() || !Player.IsLocalPlayer || !Scene.Camera.IsValid() )
		{
			return;
		}

		var camera = Scene.Camera;

		// Slowly orbit around the body
		_orbitAngle += OrbitSpeed * Time.Delta;

		// Calculate target camera position
		var bodyPosition = WorldPosition;
		var orbitOffset = new Vector3(
			MathF.Cos( _orbitAngle ) * CameraDistance,
			MathF.Sin( _orbitAngle ) * CameraDistance,
			CameraHeight
		);

		var targetPosition = bodyPosition + orbitOffset;

		// Check for obstacles between body and camera position
		var trace = Scene.Trace.Ray( bodyPosition, targetPosition )
			.WithoutTags( "player", Constants.RagdollTag, "trigger" )
			.Run();

		if ( trace.Hit )
		{
			// Move camera closer to avoid going through walls
			targetPosition = trace.HitPosition + trace.Normal * 10f;
		}

		var targetRotation = Rotation.LookAt( bodyPosition - targetPosition, Vector3.Up );

		// Smoothly move camera
		camera.WorldPosition = Vector3.Lerp( camera.WorldPosition, targetPosition, SmoothSpeed * Time.Delta );
		camera.WorldRotation = Rotation.Lerp( camera.WorldRotation, targetRotation, SmoothSpeed * Time.Delta );
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		// Apply smooth movement to grabbed bodies
		foreach ( var (player, body) in _currentDraggers.ToList() )
		{
			if ( !body.IsValid() || !player.IsValid() )
			{
				_currentDraggers.Remove( player );
				continue;
			}

			// Check distance - release if too far
			var distance = Vector3.DistanceBetween( body.WorldPosition, player.WorldPosition );
			if ( distance > GrabDistance * 1.25f )
			{
				_currentDraggers.Remove( player );
				continue;
			}

			// Only move the body if we own it and it's dynamic
			if ( body.IsProxy || !body.MotionEnabled )
			{
				continue;
			}

			// Calculate target position based on player's aim
			var aimRay = player.AimRay;
			var targetPosition = aimRay.Position + aimRay.Forward * GrabDistance;
			var targetTransform = new Transform( targetPosition );

			// Use smooth movement
			body.SmoothMove( targetTransform, 0.02f * MovementSmoothness, Time.Delta );
		}
	}


	[Rpc.Host]
	public void StartDrag()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:drag:start", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			return;
		}

		// Check if already grabbing this body
		if ( _currentDraggers.ContainsKey( player ) )
		{
			return;
		}

		var trace = Scene.Trace.Ray( player.AimRay, 200f )
			.IgnoreGameObject( player.GameObject )
			.WithTag( Constants.RagdollTag )
			.Run();

		if ( !trace.Hit || trace.Body is null )
		{
			return;
		}
		if ( trace.Component is not Rigidbody rigidbody )
		{
			return;
		}

		// Store the grab
		_currentDraggers[player] = rigidbody;
	}

	[Rpc.Host]
	public void StopDrag()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:drag:stop", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			return;
		}
		_currentDraggers.Remove( player );
	}

}
