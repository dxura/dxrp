using Dxura.RP.Shared;
using FixedJoint=Sandbox.FixedJoint;
using HingeJoint=Sandbox.HingeJoint;
using SpringJoint=Sandbox.SpringJoint;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.joint.name", "#tool.joint.description", "#tool.group.miscellaneous", MinimumLevel = 1 )]
public class JointTool : BaseTool
{
	[Property] [Title( "No Collide" )] public bool NoCollide { get; set; } = true;

	[Property] [Title( "Joint Type" )] public JointType Type { get; set; } = JointType.Fixed;

	public override string Attack1Control => _selectedObject.IsValid()
		? "#tool.joint.attack1.attach"
		: "#tool.joint.attack1.select";

	public override string Attack2Control => "#tool.joint.attack2";

	private GameObject? _selectedObject;
	private Vector3 _localGrabPoint; // Local position on the prop where user clicked
	private Vector3 _worldGrabPoint; // World position where user clicked

	public enum JointType
	{
		Fixed,
		Hinge,
		Spring
	}

	public override void OnEquip()
	{
		base.OnEquip();
		_selectedObject = null;
	}

	public override void OnToolUpdate()
	{
		base.OnToolUpdate();

		// Show preview when object is selected
		if ( _selectedObject.IsValid() )
		{
			var tr = PerformEyeTrace();

			// Draw sphere at the selected object's grab point
			_worldGrabPoint = _selectedObject.WorldTransform.PointToWorld( _localGrabPoint );
			Gizmo.Draw.Color = Color.Cyan;
			Gizmo.Draw.LineSphere( _worldGrabPoint, 2f );

			// If we're hovering over a valid target, show connection preview
			if ( tr.Hit )
			{
				var targetPos = tr.HitPosition;

				// Draw line from grab point to target
				Gizmo.Draw.Color = Color.Green;
				Gizmo.Draw.Line( _worldGrabPoint, targetPos );

				// Draw sphere at target position
				Gizmo.Draw.Color = Color.Yellow;
				Gizmo.Draw.LineSphere( targetPos, 2f );

				// Draw joint type indicator
				DrawJointPreview( _worldGrabPoint, targetPos, tr.Normal );
			}
		}
	}

	private void DrawJointPreview( Vector3 fromPos, Vector3 toPos, Vector3 normal )
	{
		switch ( Type )
		{
			case JointType.Fixed:
				// Draw solid connection for fixed joint
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.LineSphere( toPos, 3f );
				break;

			case JointType.Hinge:
				// Draw rotation axis for hinge
				var hingeAxis = Vector3.Up; // This matches the FromRoll(90) rotation
				Gizmo.Draw.Color = Color.Orange;
				Gizmo.Draw.Line( toPos - hingeAxis * 5f, toPos + hingeAxis * 5f );
				Gizmo.Draw.LineSphere( toPos - hingeAxis * 5f, 1f );
				Gizmo.Draw.LineSphere( toPos + hingeAxis * 5f, 1f );

				// Draw rotation arc
				Gizmo.Draw.Color = Color.Orange.WithAlpha( 0.5f );
				var perpendicular = Vector3.Cross( hingeAxis, Vector3.Forward ).Normal;
				for ( var i = 0; i < 16; i++ )
				{
					var angle1 = i / 16f * 360f;
					var angle2 = (i + 1) / 16f * 360f;
					var rot1 = Rotation.FromAxis( hingeAxis, angle1 );
					var rot2 = Rotation.FromAxis( hingeAxis, angle2 );
					var p1 = toPos + rot1 * perpendicular * 4f;
					var p2 = toPos + rot2 * perpendicular * 4f;
					Gizmo.Draw.Line( p1, p2 );
				}
				break;

			case JointType.Spring:
				// Draw spring coil
				Gizmo.Draw.Color = Color.Magenta;
				var springDir = (toPos - fromPos).Normal;
				var springLength = Vector3.DistanceBetween( fromPos, toPos );
				var coils = 8;
				var coilRadius = 2f;

				var perpSpring = Vector3.Cross( springDir, Vector3.Up ).Normal;
				if ( perpSpring.Length < 0.1f )
				{
					perpSpring = Vector3.Cross( springDir, Vector3.Forward ).Normal;
				}

				for ( var i = 0; i < coils * 4; i++ )
				{
					var t1 = i / (float)(coils * 4);
					var t2 = (i + 1) / (float)(coils * 4);
					var angle1 = t1 * coils * 360f;
					var angle2 = t2 * coils * 360f;

					var pos1 = fromPos + springDir * (springLength * t1);
					var pos2 = fromPos + springDir * (springLength * t2);

					var rot1 = Rotation.FromAxis( springDir, angle1 );
					var rot2 = Rotation.FromAxis( springDir, angle2 );

					var p1 = pos1 + rot1 * perpSpring * coilRadius;
					var p2 = pos2 + rot2 * perpSpring * coilRadius;

					Gizmo.Draw.Line( p1, p2 );
				}
				break;
		}
	}

	public override void PrimaryUseStart()
	{
		var tr = PerformEyeTrace();

		if ( !tr.Hit || !tr.GameObject.Tags.Has( Constants.ConstructTag ) )
		{
			return;
		}

		if ( !GameManager.Instance.RequestOwnership( tr.GameObject.Root ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}


		// If we already have an object selected, attach it
		if ( _selectedObject.IsValid() )
		{
			AttachToSurface( tr );
			_selectedObject = null;
			Notify.Success( "#tool.joint.attached" );
		}
		else
		{
			// Select the object
			if ( !tr.GameObject.IsValid() )
			{
				return;
			}

			if ( !tr.Body.IsValid() )
			{
				return;
			}

			// Store where we clicked on it (local space)
			_selectedObject = tr.GameObject;
			_localGrabPoint = tr.GameObject.WorldTransform.PointToLocal( tr.HitPosition );
			_worldGrabPoint = tr.HitPosition;
			Notify.Info( "#tool.joint.selected" );
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	public override void SecondaryUseStart()
	{
		if ( _selectedObject.IsValid() )
		{
			_selectedObject = null;
			Notify.Info( "#tool.joint.cleared" );

			var tr = PerformEyeTrace();
			if ( tr.Hit )
			{
				Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
			}
		}
	}

	private void AttachToSurface( SceneTraceResult tr )
	{
		if ( _selectedObject == null || _selectedObject.Tags.Has( Constants.GrabbedTag ) )
		{
			return;
		}

		if ( tr.GameObject.IsValid() && tr.GameObject.Tags.Has( Constants.GrabbedTag ) )
		{
			return;
		}

		if ( tr.GameObject.IsValid() && _selectedObject == tr.GameObject )
		{
			Notify.Error( "#tool.joint.self_attach" );
			return;
		}

		// Create the appropriate joint type
		switch ( Type )
		{
			case JointType.Fixed:
				CreateFixedJoint( _selectedObject, tr.GameObject, tr.HitPosition, _localGrabPoint );
				break;
			case JointType.Hinge:
				CreateHingeJoint( _selectedObject, tr.GameObject, tr.HitPosition, _localGrabPoint );
				break;
			case JointType.Spring:
				CreateSpringJoint( _selectedObject, tr.GameObject, tr.HitPosition, _localGrabPoint );
				break;
		}
	}

	private void CreateFixedJoint( GameObject selectedObject, GameObject targetObject, Vector3 hitPosition, Vector3 localGrabPoint )
	{
		Log.Info( $"Creating fixed joint on {selectedObject.Name}" );

		// Create the joint directly on the selected object at the grab point
		var jointGo = new GameObject( true, "FixedJoint" );
		jointGo.Parent = selectedObject;
		jointGo.LocalPosition = localGrabPoint;

		var joint = jointGo.AddComponent<FixedJoint>();
		joint.EnableCollision = !NoCollide;

		// Create anchor at the target position
		var targetAnchor = new GameObject( true, "joint_anchor" );
		if ( targetObject.IsValid() )
		{
			targetAnchor.Parent = targetObject;
		}
		targetAnchor.WorldPosition = hitPosition;

		joint.Body = targetAnchor;

		Log.Info( $"Created fixed joint: {joint.IsValid()}" );
	}

	private void CreateHingeJoint( GameObject selectedObject, GameObject targetObject, Vector3 hitPosition, Vector3 localGrabPoint )
	{
		Log.Info( $"Creating hinge joint on {selectedObject.Name}" );

		// Create the joint directly on the selected object at the grab point
		var jointGo = new GameObject( true, "HingeJoint" );
		jointGo.Parent = selectedObject;
		jointGo.LocalPosition = localGrabPoint;
		jointGo.LocalRotation = Rotation.FromRoll( 90 ); // Set hinge axis

		var joint = jointGo.AddComponent<HingeJoint>();
		joint.EnableCollision = !NoCollide;

		// Create anchor at the target position
		var targetAnchor = new GameObject( true, "joint_anchor" );
		if ( targetObject.IsValid() )
		{
			targetAnchor.Parent = targetObject;
		}
		targetAnchor.WorldPosition = hitPosition;

		joint.Body = targetAnchor;

		Log.Info( $"Created hinge joint: {joint.IsValid()}" );
	}

	private void CreateSpringJoint( GameObject selectedObject, GameObject targetObject, Vector3 hitPosition, Vector3 localGrabPoint )
	{
		Log.Info( $"Creating spring joint on {selectedObject.Name}" );

		// Create the joint directly on the selected object at the grab point
		var jointGo = new GameObject( true, "SpringJoint" );
		jointGo.Parent = selectedObject;
		jointGo.LocalPosition = localGrabPoint;

		var joint = jointGo.AddComponent<SpringJoint>();
		joint.EnableCollision = !NoCollide;

		// Create anchor at the target position
		var targetAnchor = new GameObject( true, "joint_anchor" );
		if ( targetObject.IsValid() )
		{
			targetAnchor.Parent = targetObject;
		}
		targetAnchor.WorldPosition = hitPosition;

		joint.Body = targetAnchor;

		// Configure spring parameters
		var worldGrabPoint = selectedObject.WorldTransform.PointToWorld( localGrabPoint );
		joint.MinLength = 0f;
		joint.MaxLength = Vector3.DistanceBetween( worldGrabPoint, hitPosition ) * 2f;
		joint.Frequency = 5.0f;

		Log.Info( $"Created spring joint: {joint.IsValid()}" );
	}
}
