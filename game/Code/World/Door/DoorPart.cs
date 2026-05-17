using Dxura.RP.Game.UI;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public sealed class DoorPart : Component, Component.IPressable, IContextualObject, IWireUsable
{
	[Property] public required Door ParentDoor { get; set; }

	[Property] public GameObject? Pivot { get; set; }
	[Property] public required Collider Collider { get; set; }
	[Property] public required List<GameObject> Handles { get; set; }

	[Property] public required GameObject Marker { get; set; }

	[Property] public Vector3 InitialPosition { get; set; }
	[Property] public Rotation InitialRotation { get; set; }
	[Property] public Vector3 PivotPoint { get; set; }

	private bool _isPlayerHovering;

	public bool LookOpacity => false;

	public Type ContextPanelTypeOverride => typeof( DoorContextPanel );
	Vector3 IContextualObject.ContextPosition => Marker.WorldPosition;

	public bool ShouldShow()
	{
		return _isPlayerHovering && ParentDoor.ContextOverlay;
	}

	public bool Press( IPressable.Event e )
	{
		ParentDoor.OnDoorPressed();
		return true;
	}

	public void OnWireUse( long owner, Vector3 userPosition )
	{
		Assert.True( Networking.IsHost );

		var isDoorOwner = FriendSystem.Instance.HasDoorPermission( ParentDoor.Owner, owner );

		ParentDoor.Toggle( userPosition, isDoorOwner );
	}

	public void Hover( IPressable.Event e )
	{
		_isPlayerHovering = true;
	}

	public void Blur( IPressable.Event e )
	{
		_isPlayerHovering = false;
	}

	protected override void OnStart()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		InitialPosition = Transform.World.Position;
		InitialRotation = Transform.World.Rotation;
		PivotPoint = Pivot?.Transform.World.Position ?? Transform.World.Position;
	}

	public void RotateHandles( float progress )
	{
		if ( Handles.Count == 0 )
		{
			return;
		}

		foreach ( var handle in Handles )
		{
			if (!handle.IsValid()) continue;
			
			var currentAngles = handle.LocalRotation.Angles();

			// Create new rotation that only modifies pitch (x-axis rotation)
			handle.LocalRotation = Rotation.From(
				progress, // pitch (x-axis)
				currentAngles.yaw, // preserve current yaw
				currentAngles.roll // preserve current roll
			);
		}
	}

	public void ResetHandles()
	{
		foreach ( var handle in Handles )
		{
			if (!handle.IsValid()) continue;
			
			var currentAngles = handle.LocalRotation.Angles();
			handle.LocalRotation = Rotation.From( 0, currentAngles.yaw, currentAngles.roll );
		}
	}

	public void AnimateRotation( float angle )
	{
		var rotation = Rotation.FromYaw( angle );
		var toPivot = InitialPosition - PivotPoint;
		var rotatedPosition = PivotPoint + rotation * toPivot;

		Transform.World = Transform.World
			.WithPosition( rotatedPosition )
			.WithRotation( InitialRotation * rotation );
	}

	public void AnimateHeight( float height )
	{
		Transform.World = Transform.World.WithPosition(
			InitialPosition + Transform.World.Rotation * (Vector3.Up * height)
		);
	}
}
