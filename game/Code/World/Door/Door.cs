using Dxura.RP.Game.Equipments;

namespace Dxura.RP.Game;

public partial class Door : Component, IHandEvents, IDescription
{
	public enum DoorState
	{
		Open,
		Closed
	}

	public enum DoorType
	{
		Single,
		Double,
		Roller
	}

	// Core Properties
	[Property] public DoorType Type { get; set; } = DoorType.Single;
	[Property] public required DoorPart MainDoor { get; set; }
	[Property] public DoorPart? SecondDoor { get; set; }

	[Property] public bool HasHandles { get; set; } = true;
	[Property] public bool ContextOverlay { get; set; } = true;

	// State Management
	[Property]
	public DoorState State { get; set; } = DoorState.Closed;

	private DoorState DefaultState { get; set; } = DoorState.Closed;

	// IDescription implementation
	public string? DisplayName => "#roleplay.door.name";
	public Color Color => Color.White;

	protected override void OnStart()
	{
		// Don't allow owner groups/jobs if jobs are disabled
		if ( !Config.Current.Game.JobsEnabled )
		{
			OwnerGroupIdentifier = null;
			OwnerJobIdentifier = null;
		}

		DefaultState = State;

		// On host, snap to open position if default state is open
		if ( Networking.IsHost && State != DoorState.Closed )
		{
			AnimateDoor( 1.0f );
		}

		if ( !HasHandles )
		{
			ClearHandles();
		}
	}

	protected override void OnFixedUpdate()
	{
		UpdateHandleAnimation();
		UpdateDoorAnimation();
	}

	private bool IsPlayerInReach( Player player )
	{
		return player.WorldPosition.Distance( WorldPosition ) <= Config.Current.Game.ReachDistance;
	}

	private void ClearHandles()
	{
		// Clear handles for MainDoor
		foreach ( var doorHandle in MainDoor.Handles )
		{
			doorHandle.Destroy();
		}

		MainDoor.Handles.Clear();

		// Clear handles for SecondDoor if it exists
		if ( SecondDoor != null )
		{
			foreach ( var doorHandle in SecondDoor.Handles )
			{
				doorHandle.Destroy();
			}

			SecondDoor.Handles.Clear();
		}
	}
}
