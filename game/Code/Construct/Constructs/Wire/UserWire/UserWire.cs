using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Wire;

[Title( "User" )]
[Category( "Wire" )]
[Icon( "cable" )]
public class UserWire() : BaseWireConstruct( ConstructType.UserWire )
{
	private UserWireData _data = new();

	[Property] public GameObject EndLaserTarget { get; set; } = null!;

	[Property] public LineRenderer LineRenderer { get; set; } = null!;

	[WireInput( "use" )]
	public bool Use
	{
		set
		{
			if ( value )
			{
				TriggerUse();
			}
		}
		get => false; // This is just a trigger, no need to store state
	}

	public override string Name => "User";

	protected override void OnStart()
	{
		base.OnStart();
		UpdateLaserVisibility();
	}

	public override void OnOcclusionChanged( bool occlude )
	{
		base.OnOcclusionChanged( occlude );
		UpdateLaserVisibility( occlude );
	}

	private void UpdateLaserVisibility( bool? occluded = null )
	{
		// Hide the laser to save performance if we're occluded
		LineRenderer.Enabled = !(occluded ?? Tags.Contains( Constants.OccludeTag )) && !GameManager.IsHeadless;
	}

	private void TriggerUse()
	{
		// Quick cooldown to prevent spamming
		if ( Cooldown.Current.CheckAndStartCooldown( $"user:{Owner}:{GameObject.Id}", Config.Current.Game.WireUserCooldown ) )
		{
			// Warn user if they exceed the global action cooldown (without spamming)
			if ( Cooldown.Current.CheckAndStartCooldown( $"user:{Owner}", Config.Current.Game.ActionCooldown ) )
			{
				var ownerPlayer = GameUtils.GetPlayerById( Owner );
				if ( ownerPlayer.IsValid() )
				{
					ownerPlayer.Warn( $"Your wire user exceeded rate limit of {Config.Current.Game.WireUserCooldown}s" );
				}
			}

			return;
		}

		// Perform raycast to check for usable objects
		var trace = Scene.Trace.Ray( WorldPosition + WorldRotation.Up * 2.5f, EndLaserTarget.WorldPosition )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( !trace.Hit )
		{
			return;
		}

		var hit = trace.GameObject;
		if ( hit == null || !hit.IsValid() )
		{
			return;
		}

		var usable = hit.Components.Get<IWireUsable>();

		usable?.OnWireUse( Owner, WorldPosition );
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as UserWireData ?? new UserWireData();

		if ( EndLaserTarget.IsValid() )
		{
			EndLaserTarget.WorldPosition = WorldPosition + WorldRotation.Up * _data.Range;
		}
	}
}
