using Dxura.RP.Game.UI;
namespace Dxura.RP.Game.Entities;

[Title( "Dry Rack" )]
[Category( "Entities" )]
public class DryRackEntity : BaseEntity, Component.ITriggerListener, IGameEvents
{
	[Property]
	private float DryTime { get; set; } = 140f;

	[Property]
	private List<GameObject> DrySlots { get; set; } = new();

	[Property]
	public required GameObject DriedWeedGameObject { get; set; }

	[Sync( SyncFlags.FromHost )]
	private TimeUntil? NextHarvestIn { get; set; }

	[Sync( SyncFlags.FromHost )]
	private int OccupiedSlots { get; set; }

	[Property]
	private List<TimeUntil?> SlotDryTimes { get; set; } = new();

	protected override void OnStart()
	{
		base.OnStart();

		if ( !Networking.IsHost )
		{
			return;
		}

		RefreshSlotState();
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		EnsureSlotTimes();

		var changed = false;
		for ( var i = 0; i < DrySlots.Count; i++ )
		{
			var slot = DrySlots[i];
			if ( !slot.IsValid() || !HasItem( i ) )
			{
				continue;
			}

			if ( !IsReadyToHarvest( i ) )
			{
				continue;
			}

			changed = true;
			SlotDryTimes[i] = null;
			SetSlotRendering( slot, false );

			var toSpawn = DriedWeedGameObject.Clone();
			toSpawn.WorldPosition = slot.WorldPosition + Vector3.Down * 8f;

			var weedEntity = toSpawn.GetComponent<WeedHarvestEntity>();
			if ( weedEntity.IsValid() )
			{
				weedEntity.Dried = true;
			}

			weedEntity.Owner = Owner;

			toSpawn.NetworkSpawn();
		}

		if ( changed )
		{
			RefreshSlotState();
		}
	}

	public void OnTriggerEnter( GameObject other )
	{
		if ( !Networking.IsHost || DrySlots.Count == 0 )
		{
			return;
		}

		var weedHarvest = other.GetComponent<WeedHarvestEntity>();
		if ( !weedHarvest.IsValid() || weedHarvest.Dried )
		{
			return;
		}

		EnsureSlotTimes();

		var availableSlotIndex = -1;
		for ( var i = 0; i < DrySlots.Count; i++ )
		{
			if ( !HasItem( i ) )
			{
				availableSlotIndex = i;
				break;
			}
		}

		if ( availableSlotIndex < 0 )
		{
			return;
		}

		var availableSlot = DrySlots[availableSlotIndex];
		SlotDryTimes[availableSlotIndex] = DryTime;

		other.Destroy();
		SetSlotRendering( availableSlot, true );
		RefreshSlotState();
	}

	private bool HasItem( int index ) => SlotDryTimes[index].HasValue;

	private bool IsReadyToHarvest( int index ) => SlotDryTimes[index] is { } dryTime && dryTime;

	private void EnsureSlotTimes()
	{
		while ( SlotDryTimes.Count < DrySlots.Count )
		{
			SlotDryTimes.Add( null );
		}

		while ( SlotDryTimes.Count > DrySlots.Count )
		{
			SlotDryTimes.RemoveAt( SlotDryTimes.Count - 1 );
		}
	}

	private void RefreshSlotState()
	{
		EnsureSlotTimes();
		float? nextHarvest = null;

		for ( var i = 0; i < DrySlots.Count; i++ )
		{
			var slot = DrySlots[i];
			if ( !slot.IsValid() )
			{
				SlotDryTimes[i] = null;
				continue;
			}

			var isDrying = HasItem( i );

			if ( SlotDryTimes[i] is { } dryTime && !dryTime )
			{
				var remaining = dryTime.Relative;
				nextHarvest = nextHarvest.HasValue ? Math.Min( nextHarvest.Value, remaining ) : remaining;
			}

			SetSlotRendering( slot, isDrying );
		}

		OccupiedSlots = SlotDryTimes.Count( t => t.HasValue );
		NextHarvestIn = nextHarvest;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void SetSlotRendering( GameObject slot, bool isEnabled )
	{
		if ( !slot.IsValid() )
		{
			return;
		}

		var renderer = slot.GetComponent<Renderer>( true );
		if ( renderer.IsValid() )
		{
			renderer.Enabled = isEnabled;
		}
	}

	public override string DisplayName
	{
		get
		{
			if ( OccupiedSlots == 0 || NextHarvestIn == null )
			{
				return $"Dry Rack (0/{DrySlots.Count})";
			}

			return $"Dry Rack ({OccupiedSlots}/{DrySlots.Count}) - {Math.Floor( NextHarvestIn.Value.Relative )}s";
		}
	}
}
