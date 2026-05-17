namespace Dxura.RP.Game.Addons.Official;

using Equipments;

[Title( "Basketball" )]
[Category( "Entities" )]
public sealed class BasketballEntity : BaseEntity, IHandEvents, Component.IPressable
{
	[Property] [Group( "Sound Settings" )] public SoundEvent? BounceSound { get; set; }
	[Property] [Group( "Sound Settings" )] [Range( 0.01f, 1.0f )] public float MinBounceVolume { get; set; } = 0.01f;
	[Property] [Group( "Sound Settings" )] [Range( 0.01f, 2.0f )] public float MaxBounceVolume { get; set; } = 2f;

	[Property] [Group( "Physics" )] [Range( 0.1f, 1.0f )] public float Bounciness { get; set; } = 0.777f;
	[Property] [Group( "Physics" )] [Range( 0.0f, 1.0f )] public float FloorFriction { get; set; } = 0.35f;
	[Property] [Group( "Physics" )] [Range( 0.0f, 1.0f )] public float WallFriction { get; set; } = 0.9f;
	[Property] [Group( "Physics" )] [Range( 0.1f, 2.0f )] public float TraceDistanceMultiplier { get; set; } = 0.5f;

	[Property] [Group( "Bounce Detection" )] [Range( 0.01f, 0.5f )] public float BounceCooldown { get; set; } = 0.1f;
	[Property] [Group( "Bounce Detection" )] [Range( 5f, 100f )] public float MinBounceSpeed { get; set; } = 5f;
	[Property] [Group( "Bounce Detection" )] [Range( -1.0f, 0.0f )] public float MinBounceAngle { get; set; } = -0.5f;
	[Property] [Group( "Bounce Detection" )] [Range( 1f, 10f )] public float RollingSpeedThreshold { get; set; } = 2.5f;
	[Property] [Group( "Bounce Detection" )] [Range( 0f, 90f )] public float FloorAngleThreshold { get; set; } = 80f;

	[Property] [Group( "Dribbling" )] [Range( 0.05f, 0.5f )] public float DribbleCooldown { get; set; } = 0.15f;
	[Property] [Group( "Dribbling" )] [Range( 100f, 1000f )] public float DribbleForce { get; set; } = 325f;
	[Property] [Group( "Dribbling" )] [Range( 10f, 100f )] public float DribbleHeight { get; set; } = 15f;
	[Property] [Group( "Dribbling" )] [Range( 0.0f, 3.0f )] public float MovementInfluence { get; set; } = 0.8f;
	[Property] [Group( "Dribbling" )] [Range( 0.0f, 1.0f )] public float AimInfluence { get; set; } = 0.9f;
	[Property] [Group( "Dribbling" )] [Range( 1f, 50f )] public float MinMovementSpeed { get; set; } = 1f;
	[Property] [Group( "Dribbling" )] [Range( 10f, 200f )] public float DribbleDetectionRange { get; set; } = 65f;

	[Property] [Group( "Throwing" )] [Range( 200f, 1500f )] public float ThrowSpeed { get; set; } = 800f;
	[Property] [Group( "Throwing" )] [Range( 10f, 300f )] public float DropSpeed { get; set; } = 50f;
	[Property] [Group( "Throwing" )] [Range( 0f, 200f )] public float BackspinStrength { get; set; } = 60f;

	private TimeSince _lastBounce = 10f;
	private TimeSince _lastVelocityCheck = 0f;
	private float FloorDetectionThreshold => MathF.Cos( FloorAngleThreshold * MathF.PI / 180f );
	private bool _wasGrabbed = false;
	private float _cachedRadius;
	private readonly SceneTraceResult[] _traceResults = new SceneTraceResult[3];

	private float CurrentRadius => _cachedRadius * WorldScale.x;

	private bool _isDribbling = false;
	private TimeSince _lastContinuousDribble = 0f;
	private readonly float _continuousDribbleInterval = 0.25f;

	protected override void OnStart()
	{
		base.OnStart();
		_cachedRadius = Components.Get<SphereCollider>()?.Radius ?? 7.5f;

		if ( Rigidbody.IsValid() )
		{
			Rigidbody.Gravity = true;
			Rigidbody.MotionEnabled = true;
		}

		GameObject.Tags.Add( Constants.EntityTag );
	}

	public bool Press( IPressable.Event e )
	{
		if ( IsGrabbed() || Cooldown.Current.CheckAndStartCooldown( $"basketball:dribble:{Connection.Local?.Id ?? Guid.Empty}", DribbleCooldown ) )
		{
			return false;
		}

		var player = GameUtils.GetPlayer( e.Source as Component );
		if ( player == null || !player.IsValid() )
		{
			return false;
		}

		if ( !GameObject.Network.IsOwner )
		{
			if ( !GameManager.Instance.RequestOwnership( GameObject ) )
			{
				return false;
			}
		}

		Dribble( player );
		return true;
	}

	private void Dribble( Player player )
	{
		if ( player == null || !player.IsValid() || IsGrabbed() || !Rigidbody.IsValid() )
		{
			return;
		}

		var playerVel = player.Controller?.Velocity ?? Vector3.Zero;
		var horizontal = new Vector3( playerVel.x, playerVel.y, 0 );

		var isPlayerMoving = horizontal.Length > MinMovementSpeed;
		var isContinuousDribble = _isDribbling && _lastContinuousDribble < _continuousDribbleInterval;

		if ( isContinuousDribble && !isPlayerMoving )
		{
			var currentVel = Rigidbody.Velocity;
			var dampedHorizontal = new Vector3( currentVel.x, currentVel.y, 0 ) * 0.3f;
			var dribbleVel = Vector3.Down * DribbleForce;
			dribbleVel = new Vector3( dampedHorizontal.x, dampedHorizontal.y, dribbleVel.z );

			Rigidbody.Velocity = dribbleVel;
			Rigidbody.AngularVelocity = Vector3.Zero;
			return;
		}

		var baseDribbleVel = Vector3.Down * DribbleForce;

		if ( isPlayerMoving )
		{
			var movementDirection = horizontal.Normal;
			var playerForward = new Vector3( player.AimRay.Forward.x, player.AimRay.Forward.y, 0 ).Normal;
			var playerRight = Vector3.Cross( playerForward, Vector3.Up ).Normal;

			var forwardDot = Vector3.Dot( movementDirection, playerForward );
			var rightDot = Vector3.Dot( movementDirection, playerRight );

			var adjustedInfluence = MovementInfluence;

			if ( Math.Abs( forwardDot ) > Math.Abs( rightDot ) )
			{
				if ( forwardDot > 0 )
				{
					adjustedInfluence *= 1.48f; // Forwards
				}
				else
				{
					adjustedInfluence *= 1.67f; // Backwards
				}
			}
			else
			{
				adjustedInfluence *= 1.42f; // Sideways
			}

			var movementComponent = horizontal * adjustedInfluence;
			baseDribbleVel += movementComponent;

			var isPureSideways = Math.Abs( forwardDot ) < 0.2f && Math.Abs( rightDot ) > 0.8f;

			if ( !isPureSideways )
			{
				var aimHorizontal = new Vector3( player.AimRay.Forward.x, player.AimRay.Forward.y, 0 ).Normal;
				var aimDifference = aimHorizontal - movementDirection;

				if ( aimDifference.Length > 0.1f )
				{
					var isDiagonal = Math.Abs( forwardDot ) > 0.3f && Math.Abs( rightDot ) > 0.3f;
					if ( isDiagonal )
					{
						var turningComponent = aimDifference * horizontal.Length * AimInfluence * 0.35f;
						baseDribbleVel += turningComponent;
					}
				}
			}
		}
		else
		{
			baseDribbleVel = FindNearestSurface( player ) * DribbleForce;
		}

		if ( Rigidbody.IsValid() && Rigidbody.MotionEnabled )
		{
			Rigidbody.Velocity = baseDribbleVel;
			Rigidbody.AngularVelocity = Vector3.Zero;
		}
	}

	private void HandleContinuousDribbling()
	{
		var localPlayer = Player.Local;
		if ( !localPlayer.IsValid() || !GameObject.Network.IsOwner )
		{
			_isDribbling = false;
			return;
		}

		var isHoldingE = Input.Down( "use" );
		var distanceToPlayer = WorldPosition.Distance( localPlayer.WorldPosition );
		var isCloseEnough = distanceToPlayer < 80f;

		if ( isHoldingE && isCloseEnough && !IsGrabbed() )
		{
			if ( !_isDribbling )
			{
				_isDribbling = true;
				_lastContinuousDribble = 0f;
			}

			if ( _lastContinuousDribble > _continuousDribbleInterval )
			{
				var playerVel = localPlayer.Controller?.Velocity ?? Vector3.Zero;
				var isPlayerMoving = new Vector3( playerVel.x, playerVel.y, 0 ).Length > MinMovementSpeed;
				var ballVel = Rigidbody.IsValid() ? Rigidbody.Velocity : Vector3.Zero;

				if ( isPlayerMoving || ballVel.Length < 50f || ballVel.Length > 5f )
				{
					Dribble( localPlayer );
					_lastContinuousDribble = 0f;
				}
			}
		}
		else
		{
			_isDribbling = false;
		}
	}

	private Vector3 FindNearestSurface( Player player )
	{
		var pos = WorldPosition;
		var bestDirection = Vector3.Down;
		var bestScore = float.MinValue;
		var playerAim = player.AimRay.Forward;

		var directions = new Vector3[]
		{
			Vector3.Down,
			(Vector3.Down + Vector3.Forward).Normal,
			(Vector3.Down + Vector3.Backward).Normal,
			(Vector3.Down + Vector3.Left).Normal,
			(Vector3.Down + Vector3.Right).Normal,
			Vector3.Forward,
			Vector3.Backward,
			Vector3.Left,
			Vector3.Right
		};

		Vector3? preferredDirection = null;
		var bestAimAlignment = 0.5f;

		foreach ( var dir in directions )
		{
			var trace = Scene.Trace.Ray( pos, pos + dir * DribbleDetectionRange ).WithoutTags( Constants.PlayerTag, Constants.NoCollideTag ).IgnoreGameObject( GameObject ).Run();
			if ( trace.Hit )
			{
				var aimAlignment = Vector3.Dot( dir, playerAim );
				if ( aimAlignment > bestAimAlignment && Vector3.Dot( dir, Vector3.Up ) > -0.7f )
				{
					bestAimAlignment = aimAlignment;
					preferredDirection = dir;
				}
			}
		}

		foreach ( var dir in directions )
		{
			var trace = Scene.Trace.Ray( pos, pos + dir * DribbleDetectionRange ).WithoutTags( Constants.PlayerTag, Constants.NoCollideTag ).IgnoreGameObject( GameObject ).Run();
			if ( trace.Hit )
			{
				var distanceScore = (DribbleDetectionRange - trace.Distance) / DribbleDetectionRange;
				var downwardScore = -Vector3.Dot( dir, Vector3.Up );
				var surfaceScore = Vector3.Dot( trace.Normal, Vector3.Up );
				var aimScore = Vector3.Dot( dir, playerAim );

				var totalScore = preferredDirection.HasValue && Vector3.Dot( dir, preferredDirection.Value ) > 0.9f
					? distanceScore * 0.1f + downwardScore * 0.1f + surfaceScore * 0.05f + aimScore * 0.75f
					: distanceScore * 0.15f + downwardScore * 0.5f + surfaceScore * 0.25f + aimScore * 0.1f;

				if ( totalScore > bestScore )
				{
					bestScore = totalScore;
					bestDirection = dir;
				}
			}
		}

		return bestDirection;
	}

	private bool IsGrabbed()
	{
		return GameObject.Tags.Has( Constants.GrabbedTag );
	}

	private void DetectAndLimitThrowForces()
	{
		if ( !Rigidbody.IsValid() )
		{
			return;
		}

		var isGrabbed = IsGrabbed();

		if ( _wasGrabbed && !isGrabbed )
		{
			var vel = Rigidbody.Velocity;
			var angVel = Rigidbody.AngularVelocity;

			if ( GameObject.Network.IsOwner )
			{
				ProcessRelease( vel, angVel );
			}
		}
		_wasGrabbed = isGrabbed;
	}

	private void ProcessRelease( Vector3 velocity, Vector3 angularVelocity )
	{
		if ( !Rigidbody.IsValid() || !Rigidbody.MotionEnabled )
		{
			return;
		}

		var vel = velocity;
		var angVel = angularVelocity;

		var isThrow = vel.Length > 50f;

		if ( vel.Length > (isThrow ? ThrowSpeed : DropSpeed) )
		{
			vel = vel.Normal * (isThrow ? ThrowSpeed : DropSpeed);
		}

		if ( isThrow )
		{
			var horizontal = new Vector3( vel.x, vel.y, 0 );
			if ( horizontal.Length > 0.1f )
			{
				var backspinAmount = Math.Min( vel.Length / ThrowSpeed, 1f ) * BackspinStrength;
				var forwardSpinAxis = Vector3.Cross( horizontal.Normal, Vector3.Up );
				var backspinRotation = forwardSpinAxis * backspinAmount;

				angVel = backspinRotation + angVel;
			}
		}

		Rigidbody.Velocity = vel;
		Rigidbody.AngularVelocity = angVel;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void BroadcastBounceSound( Vector3 position, float volume, float intensity )
	{
		if ( BounceSound?.IsValid() == true )
		{
			var handle = BounceSound.Play( position );
			if ( handle.IsValid() )
			{
				handle.Volume = volume;
				handle.Pitch = Math.Clamp( 0.8f + intensity * 0.4f, 0.8f, 1.2f );
			}
		}
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Unreliable )]
	private void PlayBounceSoundHost( Vector3 position, float volume, float intensity )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:ball:bounce", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		BroadcastBounceSound( position, volume, intensity );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsProxy || IsGrabbed() || !Rigidbody.IsValid() || Application.IsHeadless )
		{
			return;
		}

		var vel = Rigidbody.Velocity;
		var pos = WorldPosition;

		if ( vel.Length > 0.5f && _lastBounce > BounceCooldown && !IsGrabbed() )
		{
			var velDir = vel.Normal;
			var traceDist = CurrentRadius + vel.Length * Time.Delta * TraceDistanceMultiplier;
			var offset = CurrentRadius * 0.05f;

			_traceResults[0] = Scene.Trace.Ray( pos, pos + velDir * traceDist ).WithoutTags( Constants.PlayerTag, Constants.NoCollideTag ).IgnoreGameObject( GameObject ).Run();
			_traceResults[1] = Scene.Trace.Ray( pos + Vector3.Up * offset, pos + Vector3.Up * offset + velDir * traceDist ).WithoutTags( Constants.PlayerTag, Constants.NoCollideTag ).IgnoreGameObject( GameObject ).Run();
			_traceResults[2] = Scene.Trace.Ray( pos + Vector3.Down * offset, pos + Vector3.Down * offset + velDir * traceDist ).WithoutTags( Constants.PlayerTag, Constants.NoCollideTag ).IgnoreGameObject( GameObject ).Run();

			foreach ( var result in _traceResults )
			{
				if ( result.Hit )
				{
					var speed = vel.Length;
					var normal = result.Normal;
					var dotProduct = Vector3.Dot( vel, normal );

					if ( dotProduct < MinBounceAngle && speed > MinBounceSpeed )
					{
						var impactEnergy = 0.5f * speed * speed;
						var isFloor = Vector3.Dot( normal, Vector3.Up ) > FloorDetectionThreshold;
						var friction = isFloor ? FloorFriction : WallFriction;
						var bounceEnergy = impactEnergy * Bounciness * (1f - friction);

						if ( speed < 5f )
						{
							bounceEnergy = Math.Min( bounceEnergy, 0.5f * (DribbleHeight * DribbleHeight) );
						}

						var bounceSpeed = (float)Math.Sqrt( 2f * bounceEnergy );
						var reflectedVelocity = (vel - 2 * dotProduct * normal).Normal * bounceSpeed;

						var angularVel = Rigidbody.AngularVelocity;
						if ( angularVel.Length > 0.5f && isFloor )
						{
							var spinDirection = Vector3.Cross( angularVel.Normal, Vector3.Up );
							var spinInfluence = spinDirection * bounceSpeed * 0.15f;
							reflectedVelocity += spinInfluence;

							if ( speed > 20f )
							{
								Rigidbody.AngularVelocity = angularVel * 0.8f;
							}
						}

						if ( Rigidbody.IsValid() && Rigidbody.MotionEnabled )
						{
							Rigidbody.Velocity = reflectedVelocity;

							if ( !(angularVel.Length > 0.5f && isFloor) )
							{
								var horizontal = new Vector3( reflectedVelocity.x, reflectedVelocity.y, 0 );
								if ( horizontal.Length > RollingSpeedThreshold )
								{
									Rigidbody.AngularVelocity = Vector3.Cross( Vector3.Up, horizontal.Normal ) * (horizontal.Length / CurrentRadius);
								}
							}
						}

						_lastBounce = 0f;

						var intensity = speed / 300f;
						var volume = Math.Clamp( intensity, MinBounceVolume, MaxBounceVolume );
						PlayBounceSoundHost( WorldPosition, volume, intensity );
						break;
					}
				}
			}
		}

		if ( vel.Length > 0.1f && !IsGrabbed() )
		{
			var horizontal = new Vector3( vel.x, vel.y, 0 );
			if ( horizontal.Length > 1f && (Math.Abs( vel.z ) < 10f || vel.z > -5f) )
			{
				var currentAngularSpeed = Rigidbody.AngularVelocity.Length;
				var targetAngularVel = Vector3.Cross( Vector3.Up, horizontal.Normal ) * (horizontal.Length / CurrentRadius);

				if ( currentAngularSpeed < 20f )
				{
					Rigidbody.AngularVelocity = Vector3.Lerp( Rigidbody.AngularVelocity, targetAngularVel, Time.Delta * 5f );
				}
			}
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( IsProxy )
		{
			return;
		}

		if ( _lastVelocityCheck > 0.1f )
		{
			DetectAndLimitThrowForces();
			_lastVelocityCheck = 0f;
		}

		HandleContinuousDribbling();
	}

	public override bool CanScale( Player player )
	{
		if ( !this.IsValid() || !player.IsValid() )
		{
			return false;
		}
		
		return player.SteamId == Owner && GameUtils.HasPermission( player.SteamId, GameObject );
	}

}
