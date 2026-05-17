using Dxura.RP.Game.UI;
namespace Dxura.RP.Game.Entities;

[Title( "Planter" )]
[Category( "Entities" )]
public sealed class PlanterEntity : BaseEntity, Component.ITriggerListener, IContextualObject, Component.IPressable, IGameEvents
{
	[Property]
	[Sync( SyncFlags.FromHost )]
	public PlantResource? Plant { get; set; }

	[Property]
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnSoiledChanged ) )]
	public bool Soiled { get; set; } = false;

	[Property]
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnWateredChanged ) )]
	public bool Watered { get; set; } = false;

	[Property]
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnStageChanged ) )]
	public int Stage { get; set; } = 0;

	[Property]
	[Sync( SyncFlags.FromHost )]
	public float GrowthProgress { get; set; }

	[Property]
	[Sync( SyncFlags.FromHost )]
	public bool HasLight { get; set; } = false;

	public bool IsMature => Stage >= Plant?.Stages.Count;

	public float TotalGrowthPercentage
	{
		get
		{
			if ( !Plant.IsValid() || Plant.Stages.Count == 0 )
			{
				return 0f;
			}

			return (Stage + GrowthProgress) / Plant.Stages.Count * 100f;
		}
	}

	[Property]
	private ModelRenderer ModelRenderer { get; set; } = null!;

	private readonly object _harvestLock = new();

	private TimeSince _lastLightCheck = 0f;
	private TimeSince _lastGrowth = 0f;

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( _lastLightCheck > (HasLight ? 30f : 5f) )
		{
			_lastLightCheck = 0f;

			// Growlight detection
			var nearbyGameObjects = Scene.FindInPhysics( new Sphere( WorldPosition, 100 ) );
			var hasGrowLight = nearbyGameObjects.Any( x => x.Tags.Has( "UV" ) );

			// Sunlight detection
			var startPosition = WorldPosition + Vector3.Up * 20f;
			var tr = Scene.Trace.Ray( startPosition, startPosition + Vector3.Up * 1000f )
				.Size( 2f )
				.IgnoreGameObject( GameObject )
				.Run();

			var hasSunlight = !tr.Hit;

			HasLight = hasSunlight || hasGrowLight;
		}


		if ( Plant == null || !Soiled || !Watered || !HasLight || Stage >= Plant.Stages.Count )
		{
			_lastGrowth = 0f;
			GrowthProgress = 0f;
			return;
		}

		if ( Plant.TimeToGrow <= 0 )
		{
			return;
		}

		// Update growth progress smoothly
		GrowthProgress = Math.Clamp( _lastGrowth / Plant.TimeToGrow, 0f, 1f );

		// When we reach full progress, advance to next stage
		if ( GrowthProgress >= 1f )
		{
			Stage += 1;
			GrowthProgress = 0f;
			_lastGrowth = 0f;
		}
	}

	public bool Press( IPressable.Event e )
	{
		if ( !IsMature )
		{
			Notify.Error( "Not ready for harvest" );
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "harvest", Config.Current.Game.PlanterHarvestCooldown, true ) )
		{
			return false;
		}

		OnHarvestHost();

		return true;
	}

	[Rpc.Host]
	private void OnHarvestHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:harvest", Config.Current.Game.PlanterHarvestCooldown ) )
		{
			return;
		}

		var harvesterPlayer = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !harvesterPlayer.IsValid() )
		{
			return;
		}

		lock ( _harvestLock )
		{
			var harvest = Plant?.Harvest;

			if ( !IsMature || harvest == null )
			{
				return;
			}

			var bounty = harvest.Clone();

			bounty.WorldPosition = WorldPosition + Vector3.Up * 75f;
			bounty.WorldRotation = WorldRotation;

			var entity = bounty.GetComponent<BaseEntity>();

			if ( entity.IsValid() && harvesterPlayer.IsValid() )
			{
				entity.Owner = harvesterPlayer.SteamId;
			}

			bounty.NetworkSpawn( harvesterPlayer.Connection );

			// Reset the plant
			Watered = false;
			Stage = 0;
			GrowthProgress = 0f;
			Plant = null;
		}
	}

	private void OnSoiledChanged( bool oldValue, bool newValue )
	{
		ModelRenderer.SetBodyGroup( "soil_stages", newValue ? 1 : 0 );
	}

	private void OnWateredChanged( bool oldValue, bool newValue )
	{
		ModelRenderer.MaterialGroup = newValue ? "watered" : "default";
	}

	private void OnStageChanged( int oldValue, int newValue )
	{
		ModelRenderer.SetBodyGroup( "plant_stages", newValue );
	}

	public void OnTriggerEnter( GameObject other )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		var container = other.GetComponent<ContainerEntity>();
		if ( !container.IsValid() )
		{
			return;
		}

		if ( container.IsEmpty )
		{
			return;
		}

		var resource = container.ContainedResource;
		switch ( container.ContainerType )
		{

			case ContainerType.Solid:
				// Dirt
				if ( resource.Identifier == "dirt" )
				{
					if ( Soiled ) // Can only be soiled once
					{
						return;
					}

					container.Quantity--;
					Soiled = true;
				}
				break;
			case ContainerType.Liquid:
				// Water
				if ( resource.Identifier == "water" )
				{
					if ( Watered || !Soiled ) // Can only be watered once (and only if soiled)
					{
						return;
					}

					container.Quantity--;
					Watered = true;
				}
				break;
			case ContainerType.Seed:

				// Plant
				var plant = PlantResource.All.FirstOrDefault( x => x.Resource == resource );

				if ( plant == null || !Soiled || Plant != null )
				{
					return;
				}

				container.Quantity--;
				Plant = plant;
				break;
		}
	}


	public Vector3 ContextPosition => WorldPosition;
	public bool LookOpacity => false;
	public float ContextMaxDistance => 90f;
	public Type ContextPanelTypeOverride => typeof( PlanterContextPanel );
}
