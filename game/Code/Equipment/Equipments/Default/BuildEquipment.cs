using Dxura.RP.Game.Commands;
using Dxura.RP.Game.Utilities;
using Dxura.RP.Shared;
namespace Dxura.RP.Game.Equipments;

public class BuildEquipment : InputWeaponComponent, IEquipmentEvents
{
	[Property] public float MinTargetDistance { get; set; } = 0.0f;
	[Property] public float MaxTargetDistance { get; set; } = 10000.0f;
	[Property] public float TargetDistanceSpeed { get; set; } = 25.0f;
	[Property] public float RotateSpeed { get; set; } = 0.125f;
	[Property] public float RotateSnapAt { get; set; } = 45.0f;

	[Property] [RequireComponent] public required LineRenderer Beam { get; set; }

	private Player? HeldPlayer { get; set; }
	private Rigidbody? HeldRigid { get; set; }
	private Vector3 HeldPos { get; set; }
	private Rotation HeldRot { get; set; }
	private Vector3 HoldPos { get; set; }
	private Rotation HoldRot { get; set; }
	private float HoldDistance { get; set; }
	[Sync] private bool Grabbing { get; set; }

	private LineRenderer? _viewModelBeam;

	[Sync] private bool BeamActive { get; set; }
	[Sync] private GameObject? GrabbedObject { get; set; }

	[Sync] private Vector3 GrabbedPos { get; set; }

	// Store original highlight properties to restore later
	private bool HadOriginalHighlight { get; set; }
	private Color OriginalHighlightColor { get; set; }
	private float OriginalHighlightWidth { get; set; }

	/// <summary>
	///     Accessor for the aim ray.
	/// </summary>
	private Ray WeaponRay => Equipment.Owner?.AimRay ?? new Ray();

	private IEquipment? Effector
	{
		get
		{
			if ( IsProxy || !Equipment.ViewModel.IsValid() )
			{
				return Equipment;
			}

			return Equipment.ViewModel;
		}
	}

	private bool _rotating;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		RenderBeam();
	}

	private void RenderBeam()
	{
		if ( GameManager.IsHeadless )
		{
			return;
		}

		// Determine which beam to use
		var isFirstPerson = !IsProxy && Equipment.ViewModel.IsValid();
		LineRenderer activeBeam;

		if ( isFirstPerson )
		{
			// Create viewmodel beam if it doesn't exist
			if ( !_viewModelBeam.IsValid() && Equipment.ViewModel.IsValid() )
			{
				_viewModelBeam = Equipment.ViewModel.GameObject.GetOrAddComponent<LineRenderer>();
				_viewModelBeam.Enabled = false;
				_viewModelBeam.Additive = Beam.Additive;
				_viewModelBeam.AutoCalculateNormals = Beam.AutoCalculateNormals;
				_viewModelBeam.CastShadows = false;
				_viewModelBeam.Color = Beam.Color;
				_viewModelBeam.CylinderSegments = Beam.CylinderSegments;
				_viewModelBeam.DepthFeather = Beam.DepthFeather;
				_viewModelBeam.FogStrength = Beam.FogStrength;
				_viewModelBeam.Lighting = Beam.Lighting;
				_viewModelBeam.Opaque = Beam.Opaque;
				_viewModelBeam.SplineInterpolation = Beam.SplineInterpolation;
				_viewModelBeam.UseVectorPoints = true;
				_viewModelBeam.Width = Beam.Width;
			}

			activeBeam = _viewModelBeam;
			// Disable world model beam in first person
			if ( Beam.IsValid() )
			{
				Beam.Enabled = false;
			}
		}
		else
		{
			activeBeam = Beam;
			// Disable viewmodel beam in third person
			if ( _viewModelBeam.IsValid() )
			{
				_viewModelBeam.Enabled = false;
			}
		}

		if ( !activeBeam.IsValid() )
		{
			return;
		}

		if ( !GrabbedObject.IsValid() || Effector?.Muzzle == null )
		{
			activeBeam.Enabled = false;
			return;
		}

		activeBeam.Enabled = true;

		// Render the beam
		var start = Effector.Muzzle.Transform.World.Position;
		var end = GrabbedObject!.Transform.Local.PointToWorld( GrabbedPos / GrabbedObject.WorldScale );
		var dir = Effector.Muzzle.Transform.World.Forward;
		var points = MathUtils.GetCurvedPoints( start, dir, end,
			(int)(MathF.Round( Vector3.DistanceBetween( start, end ) ) / 10) );

		var vectorPoints = new List<Vector3>();
		for ( var i = 0; i < points.Count; i++ )
		{
			if ( i == 0 || i == points.Count - 1 )
			{
				vectorPoints.Add( points[i] );
			}
			else
			{
				vectorPoints.Add( points[i] + Vector3.Random * 0.1f );
			}
		}

		activeBeam.VectorPoints = vectorPoints;
	}

	protected override void OnInputFixedUpdate()
	{
		var eyePos = WeaponRay.Position;
		var eyeDir = WeaponRay.Forward;
		var eyeRot = Rotation.From( new Angles( 0.0f, Equipment.Owner?.Controller.EyeAngles.yaw ?? 0, 0.0f ) );

		if ( Input.Pressed( "Attack1" ) )
		{
			Equipment.Owner?.Renderer.Set( "b_attack", true );

			if ( !Grabbing )
			{
				Grabbing = true;
			}
		}

		var grabEnabled = Grabbing && Input.Down( "Attack1" );
		var wantsToFreeze = Input.Pressed( "Attack2" );

		if ( GrabbedObject.IsValid() && wantsToFreeze )
		{
			Equipment.Owner?.Renderer.Set( "b_attack", true );
		}

		BeamActive = grabEnabled;

		if ( grabEnabled )
		{
			if ( HeldRigid.IsValid() )
			{
				UpdateGrab( eyePos, eyeRot, eyeDir, wantsToFreeze );
			}
			else
			{
				TryStartGrab( eyePos, eyeRot, eyeDir );
			}
		}
		else if ( Grabbing )
		{
			GrabEnd();
		}

		if ( BeamActive )
		{
			Input.MouseWheel = 0;
		}

		if ( Equipment.Owner != null )
		{
			Equipment.Owner.CantSwitch = GrabbedObject != null;
		}

		if ( _rotating )
		{
			_rotating = GrabbedObject != null;
		}

		if ( !Equipment.Owner.IsValid() )
		{
			return;
		}

		Equipment.Owner.LockCamera = _rotating;

		if ( IsProxy )
		{
			return;
		}

		if ( !HeldRigid.IsValid() )
		{
			return;
		}

		var velocity = HeldRigid.Velocity;
		Vector3.SmoothDamp( HeldRigid.WorldPosition, HoldPos, ref velocity, 0.075f, Time.Delta );
		HeldRigid.Velocity = velocity;

		var angularVelocity = HeldRigid.AngularVelocity;
		Rotation.SmoothDamp( HeldRigid.WorldRotation, HoldRot, ref angularVelocity, 0.075f, Time.Delta );
		HeldRigid.AngularVelocity = angularVelocity;
	}

	private void TryStartGrab( Vector3 eyePos, Rotation eyeRot, Vector3 eyeDir )
	{
		var tr = Scene.Trace.Ray( eyePos, eyePos + eyeDir * MaxTargetDistance )
			.UseHitboxes()
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.Run();

		if ( !tr.Hit || !tr.GameObject.IsValid() || !tr.GameObject.Tags.HasAny( Constants.BuildInteractTag, Constants.PlayerTag ) )
		{
			return;
		}

		tr.GameObject = tr.GameObject.Root;

		var target = tr.GameObject;

		if ( target.Tags.Contains( Constants.PlayerTag ) )
		{
			// Don't let players grab other players unless they are staff
			if ( !RankSystem.HasLocalPermission( Permission.PlayerGrab ) )
			{
				return;
			}

			HeldPlayer = target.GetComponent<Player>();
			if ( !HeldPlayer.IsValid() )
			{
				return;
			}

			// Don't allow grabbing players with a higher rank
			if ( !RankSystem.CanLocalTarget( HeldPlayer.SteamId ) )
			{
				HeldPlayer = null;
				return;
			}

			// Picking up a player runs /freeze to toggle (removing status if previously frozen)
			if ( HeldPlayer.HasStatus( Constants.FreezeStatus ) )
			{
				Chat.Current?.ExecuteCommandHost( FreezeCommand.Name, HeldPlayer.SteamId.ToString() );
			}
		}

		if ( HeldPlayer == null && !GameManager.Instance.RequestOwnership( target.Root ) )
		{
			return;
		}

		var rigidBody = target.GetOrAddComponent<Rigidbody>();
		if ( !rigidBody.IsValid() )
		{
			return;
		}

		if ( !target.Tags.Has( Constants.ConstructTag ) )
		{
			rigidBody.RigidbodyFlags |= RigidbodyFlags.DisableCollisionSounds;
		}

		GrabbedObject = target;
		GrabbedPos = target.Transform.World.PointToLocal( tr.EndPosition );

		GrabInit( rigidBody, eyePos, tr.EndPosition, eyeRot );

		// Check if the object already has a highlight component
		var existingOutline = target.GetComponent<HighlightOutline>();
		var outline = target.GetOrAddComponent<HighlightOutline>();

		if ( outline.IsValid() )
		{
			// Store original properties if highlight already existed
			HadOriginalHighlight = existingOutline.IsValid();
			if ( HadOriginalHighlight )
			{
				OriginalHighlightColor = existingOutline.Color;
				OriginalHighlightWidth = existingOutline.Width;
			}

			// Set grab highlight properties
			outline.Width = 0.5f;
			outline.Color = "#0BAEDB";
		}

		OnHeldChangedHost( target, WorldPosition, WorldRotation );
	}


	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void OnHeldChangedHost( GameObject? grabbedObject, Vector3 position, Rotation rotation, bool isGrabbing = true, bool freeze = false )
	{
		var caller = Rpc.Caller;

		if ( !grabbedObject.IsValid() )
		{
			return;
		}

		if ( !GameUtils.HasPermission( caller, grabbedObject ) )
		{
			return;
		}

		// Handle constructs
		if ( grabbedObject.Tags.Has( Constants.ConstructTag ) )
		{
			var construct = grabbedObject.GetComponent<IConstruct>();
			if ( construct.IsValid() )
			{
				if ( isGrabbing )
				{
					construct.Unfreeze();
				}
				else if ( freeze )
				{
					construct.Freeze( position, rotation );
				}
			}
		}

		GameManager.Instance.BroadcastTagHost( grabbedObject, isGrabbing, Constants.GrabbedTag );

		if ( !isGrabbing && Config.Current.Game.PreventPropExploits )
		{
			var guard = grabbedObject.GetOrAddComponent<CollideGuard>();
			guard.ResetTimer();
		}
	}

	private void UpdateGrab( Vector3 eyePos, Rotation eyeRot, Vector3 eyeDir, bool wantsToFreeze )
	{
		if ( wantsToFreeze )
		{
			if ( HeldPlayer != null )
			{
				if ( !HeldPlayer.HasStatus( Constants.FreezeStatus ) )
				{
					Chat.Current?.ExecuteCommandHost( FreezeCommand.Name, HeldPlayer.SteamId.ToString() );
				}
				GrabEnd();
				return;
			}

			GrabEnd( true );
			return;
		}

		MoveTargetDistance( Input.MouseWheel.y * TargetDistanceSpeed );

		_rotating = Input.Down( "Use" );
		var snapping = false;

		if ( _rotating )
		{
			DoRotate( eyeRot, Input.MouseDelta * RotateSpeed );
			snapping = Input.Down( "Run" );
		}

		GrabMove( eyePos, eyeDir, eyeRot, snapping );
	}

	private void GrabInit( Rigidbody rigidbody, Vector3 startPos, Vector3 grabPos, Rotation rot )
	{
		if ( !rigidbody.IsValid() )
		{
			return;
		}

		Grabbing = true;
		HeldRigid = rigidbody;
		HoldDistance = Vector3.DistanceBetween( startPos, grabPos );
		HoldDistance = HoldDistance.Clamp( MinTargetDistance, MaxTargetDistance );

		HeldRot = rot.Inverse * HeldRigid.WorldRotation;
		HeldPos = HeldRigid.PhysicsBody.Transform.PointToLocal( grabPos );

		HoldPos = HeldRigid.WorldPosition;
		HoldRot = HeldRigid.WorldRotation;
	}

	private void GrabEnd( bool freeze = false )
	{
		if ( GrabbedObject.IsValid() )
		{
			// Remove highlighting
			var grabbedObjectHighlight = GrabbedObject.GetComponent<HighlightOutline>();
			if ( grabbedObjectHighlight.IsValid() )
			{
				// If the object had an original highlight, restore it; otherwise remove the highlight
				if ( HadOriginalHighlight )
				{
					grabbedObjectHighlight.Color = OriginalHighlightColor;
					grabbedObjectHighlight.Width = OriginalHighlightWidth;
				}
				else
				{
					grabbedObjectHighlight.Destroy();
				}
			}

			if ( freeze )
			{
				var rigidbody = GrabbedObject.GetComponent<Rigidbody>();
				if ( rigidbody.IsValid() )
				{
					rigidbody.Destroy();
				}
			}

			// Remove grabbed tag immediately to prevent it from being stuck
			// This ensures the tag is removed even if the RPC fails
			GrabbedObject.Tags.Remove( Constants.GrabbedTag );

			OnHeldChangedHost( GrabbedObject, GrabbedObject.WorldPosition, GrabbedObject.WorldRotation, false, freeze );
		}

		// Reset highlight tracking state
		HadOriginalHighlight = false;


		GrabbedObject = null;

		HeldPlayer = null;
		HeldRigid = null;
		Grabbing = false;
	}

	private void GrabMove( Vector3 startPos, Vector3 dir, Rotation rot, bool snapAngles )
	{
		if ( HeldPlayer != null )
		{
			HoldPos = startPos - HeldPos * HeldPlayer.WorldRotation + dir * HoldDistance;
			AdminSystem.Instance.MovePlayerHost( HeldPlayer.SteamId, HoldPos );
			return;
		}

		if ( !HeldRigid.IsValid() )
		{
			return;
		}

		HoldPos = startPos - HeldPos * HeldRigid.WorldRotation + dir * HoldDistance;
		HoldRot = rot * HeldRot;

		if ( !snapAngles )
		{
			return;
		}

		var angles = HoldRot.Angles();

		HoldRot = Rotation.From(
			MathF.Round( angles.pitch / RotateSnapAt ) * RotateSnapAt,
			MathF.Round( angles.yaw / RotateSnapAt ) * RotateSnapAt,
			MathF.Round( angles.roll / RotateSnapAt ) * RotateSnapAt
		);
	}

	private void MoveTargetDistance( float distance )
	{
		HoldDistance += distance;
		HoldDistance = HoldDistance.Clamp( MinTargetDistance, MaxTargetDistance );
	}

	private void DoRotate( Rotation eye, Vector3 input )
	{
		var localRot = eye;
		localRot *= Rotation.FromAxis( Vector3.Up, input.x * RotateSpeed );
		localRot *= Rotation.FromAxis( Vector3.Right, input.y * RotateSpeed );
		localRot = eye.Inverse * localRot;

		HeldRot = localRot * HeldRot;
	}

	protected override void OnDisabled()
	{
		// Clean up viewmodel beam
		if ( _viewModelBeam.IsValid() )
		{
			_viewModelBeam.Destroy();
			_viewModelBeam = null;
		}

		if ( IsProxy || !GrabbedObject.IsValid() )
		{
			return;
		}

		Player.Local.CantSwitch = false;
		GrabEnd();
	}

	public new void OnEquipmentHolstered( Equipment equipment )
	{
		// Clean up viewmodel beam
		if ( _viewModelBeam.IsValid() )
		{
			_viewModelBeam.Destroy();
			_viewModelBeam = null;
		}

		if ( IsProxy || !GrabbedObject.IsValid() )
		{
			return;
		}

		Player.Local.CantSwitch = false;
		GrabEnd();
	}
}
