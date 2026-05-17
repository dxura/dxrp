namespace Dxura.RP.Game;

public partial class Door : IBreachable
{
	bool IBreachable.IsValid()
	{
		return this.IsValid();
	}

	public bool CanRepair()
	{
		return Config.Current.Game.DoorRepairEnabled;
	}

	public void BreachHost( Vector3 position )
	{
		BroadcastLocked( false );

		if ( OpenAwayFromPlayer && Type != DoorType.Roller )
		{
			var doorToPlayer = (position - WorldPosition).Normal;
			var doorForward = Transform.Local.Rotation.Forward;

			ReverseDirection = Vector3.Dot( doorToPlayer, doorForward ) > 0;
		}

		BroadcastState( DoorState.Open, ReverseDirection );
		OpenSound.Broadcast( WorldPosition );
	}
}
