using System.Threading.Tasks;

namespace Dxura.RP.Game;

/// <summary>
/// Interface handling occlusion-related events
/// </summary>
public interface IOcclusionEvents : ISceneEvent<IOcclusionEvents>
{
	void OnOccluded() {}
	void OnUnoccluded() {}

	void OnOcclusionChanged( bool occlude ) {}
}

/// <summary>
///     Shitty occlusion
/// </summary>
public class OcclusionSystem : GameObjectSystem<OcclusionSystem>
{
	private TimeSince _lastOcclusion = 0;
	private int _forceCheckRequestVersion;
	private const float OccludeInterval = 2.5f;

	[ConVar( "dx_occlusion_raycast_players", ConVarFlags.Saved )]
	private static bool RaycastPlayers { get; set; } = true;

	[ConVar( "dx_occlusion_distance", ConVarFlags.Saved, Min = 500, Max = 10000 )]
	public static int OcclusionDistance { get; set; } = 1500;

	public OcclusionSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.FinishUpdate, 1000, Process, "Occlusion" );
	}

	private void Process()
	{
		if ( Scene.IsEditor || !Scene.Camera.IsValid() || GameManager.IsHeadless || !Config.Current.Game.OcclusionEnabled || _lastOcclusion < OccludeInterval )
		{
			return;
		}

		var occludeDistanceSquared = OcclusionDistance * OcclusionDistance;

		Occlude( occludeDistanceSquared );
		OccludePlayers( occludeDistanceSquared );
		_lastOcclusion = 0;
	}

	private void OccludePlayers( int occludeDistanceSquared )
	{
		if ( !Player.Local.IsValid() )
		{
			return;
		}

		var cameraPosition = Scene.Camera.WorldPosition;

		foreach ( var player in GameUtils.Players )
		{
			if ( !player.IsValid() || player == Player.Local || player.HasStatus( Constants.CloakStatus ) || player.IsDead )
			{
				continue;
			}

			var shouldOcclude = player.WorldPosition.DistanceSquared( cameraPosition ) >= occludeDistanceSquared;

			if ( shouldOcclude && RaycastPlayers )
			{
				// Verify with raycast
				var trace = Scene.Trace.Ray( cameraPosition, player.Controller.EyePosition )
					.WithAnyTags( Constants.ConstructTag, Constants.MapTag )
					.Run();

				// We've hit the map between local camera and the player, occlude
				shouldOcclude = trace.Hit;
			}

			// No change
			if ( shouldOcclude == player.GameObject.Tags.Has( Constants.OccludeTag ) )
			{
				continue;
			}

			if ( shouldOcclude )
			{
				player.GameObject.Tags.Add( Constants.OccludeTag );
				player.GameObject.Network.Interpolation = false;
				player.Controller.Enabled = false;
				player.ModelHitboxes.Enabled = false;
				player.NamePlate.Enabled = false;
				
				if ( DxOccludeClothing )
				{
					player.ClearClothing();
				}
				
				player.Renderer.Enabled = false;
			}
			else
			{
				player.GameObject.Tags.Remove( Constants.OccludeTag );
				player.GameObject.Network.Interpolation = true;
				player.Renderer.Enabled = true;
				player.Controller.Enabled = !player.IsDebugPlayer;
				player.ModelHitboxes.Enabled = true;
				player.NamePlate.Enabled = true;
				
				if ( DxOccludeClothing )
				{
					player.ApplyClothing();
				}
			}
		}
	}

	private void Occlude( int occludeDistanceSquared )
	{
		var cameraPosition = Scene.Camera.WorldPosition;

		foreach ( var gameObject in Scene.Children )
		{
			HandleGameObjectOcclude( gameObject, cameraPosition, occludeDistanceSquared );

		}
	}

	private void HandleGameObjectOcclude( GameObject gameObject, Vector3 cameraPosition, int occludeDistanceSquared )
	{
		if ( !gameObject.Tags.Has( Constants.OccludableTag ) )
		{
			return;
		}

		var distance = cameraPosition.DistanceSquared( gameObject.WorldPosition );
		var shouldOcclude = distance >= occludeDistanceSquared * (gameObject.Tags.Has( Constants.CostlyTag ) ? 0.25f : 1.0f);

		// No change
		if ( shouldOcclude == gameObject.Tags.Has( Constants.OccludeTag ) )
		{
			return;
		}

		if ( shouldOcclude )
		{
			gameObject.Tags.Add( Constants.OccludeTag );
			IOcclusionEvents.PostToGameObject( gameObject, x => x.OnOcclusionChanged( true ) );
			IOcclusionEvents.PostToGameObject( gameObject, x => x.OnOccluded() );
		}
		else
		{
			gameObject.Tags.Remove( Constants.OccludeTag );
			IOcclusionEvents.PostToGameObject( gameObject, x => x.OnOcclusionChanged( false ) );
			IOcclusionEvents.PostToGameObject( gameObject, x => x.OnUnoccluded() );
		}
	}

	private static void Clear()
	{
		// Clear all occlusion tags
		foreach ( var gameObject in Sandbox.Game.ActiveScene.Children )
		{
			gameObject.Tags.Remove( Constants.OccludeTag );
		}

		// Render all players
		foreach ( var player in GameUtils.Players )
		{
			player.Renderer.Enabled = true;
			player.Controller.Enabled = true;
			player.GameObject.Tags.Remove( Constants.OccludeTag );
		}
	}

	public void ForceCheckGameObject( GameObject gameObject )
	{
		if ( !Config.Current.Game.OcclusionEnabled || !Scene.Camera.IsValid() )
		{
			return;
		}

		var cameraPosition = Scene.Camera.WorldPosition;
		var occludeDistanceSquared = OcclusionDistance * OcclusionDistance;

		HandleGameObjectOcclude( gameObject, cameraPosition, occludeDistanceSquared );
	}

	public void ForceCheck()
	{
		if ( !Config.Current.Game.OcclusionEnabled || !Scene.Camera.IsValid() )
		{
			return;
		}

		var occludeDistanceSquared = OcclusionDistance * OcclusionDistance;

		Occlude( occludeDistanceSquared );
		OccludePlayers( occludeDistanceSquared );
		_lastOcclusion = 0;
	}

	public void RequestForceCheck()
	{
		var requestVersion = ++_forceCheckRequestVersion;
		_ = ForceCheckSoon( requestVersion );
	}

	public void BroadcastForceCheckHost( params Connection?[] connections )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		var recipients = connections
			.Where( connection => connection != null )
			.Distinct()
			.ToArray();

		if ( recipients.Length == 0 )
		{
			return;
		}

		using ( Rpc.FilterInclude( connection => recipients.Contains( connection ) ) )
		{
			BroadcastForceCheck();
		}
	}

	private async Task ForceCheckSoon( int requestVersion )
	{
		foreach ( var delay in new[] { 1, 16, 100, 250 } )
		{
			await GameTask.Delay( delay );
			await GameTask.MainThread();

			if ( requestVersion != _forceCheckRequestVersion || !Scene.Camera.IsValid() )
			{
				return;
			}

			ForceCheck();
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastForceCheck()
	{
		RequestForceCheck();
	}

	[ConCmd( "dx_toggle_occlusion" )]
	private static void ToggleOcclusion()
	{
		Config.Current.Game.OcclusionEnabled = !Config.Current.Game.OcclusionEnabled;

		// If occlusion is enabled let loop handle, otherwise we need to clear all occlusion tags ourselves
		if ( Config.Current.Game.OcclusionEnabled )
		{
			return;
		}

		Clear();
	}
	
	[ConVar( "dx_occlude_clothing", ConVarFlags.Saved )]
	private static bool DxOccludeClothing { get; set; } = false;
}
