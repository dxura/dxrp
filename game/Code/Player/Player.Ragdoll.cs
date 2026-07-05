using System.Threading.Tasks;
using Dxura.RP.Game.UI;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public partial class Player
{
	private const float PlayerRagdollSyncInterval = 0.5f;
	private const float StunRagdollImpulseScale = 0.65f;
	private TimeSince _lastRagdollPlayerSync = 0f;
	private TimeSince _lastStunRagdollPlayerSync = 0f;
	private GameObject? _ragdollGameObject;
	private GameObject? _stunRagdollGameObject;
	private int _stunRagdollGeneration;

	/// <summary>
	/// Create a ragdoll GameObject that matches our current rendered body.
	/// </summary>
	public GameObject CreateRagdoll( string name = "Ragdoll" )
	{
		var ragdoll = new GameObject( true, name );
		ragdoll.Tags.Add( Constants.RagdollTag );
		ragdoll.WorldTransform = WorldTransform;

		if ( !Renderer.IsValid() )
		{
			return ragdoll;
		}

		// Main skinned renderer (body)
		var ragdollRenderer = ragdoll.GetOrAddComponent<SkinnedModelRenderer>();
		ragdollRenderer.Model = GetRagdollBodyModel();
		ragdollRenderer.UseAnimGraph = false;
		ragdollRenderer.Enabled = true;


		// Physics
		var modelPhysics = ragdoll.Components.Create<ModelPhysics>();
		modelPhysics.Model = ragdollRenderer.Model;
		modelPhysics.Renderer = ragdollRenderer;
		modelPhysics.RigidbodyFlags |= RigidbodyFlags.DisableCollisionSounds;

		return ragdoll;
	}

	private Model GetRagdollBodyModel()
	{
		// Prefer job model so host/headless doesn't depend on the live renderer state.
		if ( Job.IsValid() )
		{
			return Job.GetPrimaryModel();
		}

		return Renderer.Model;
	}

	private void ClearRagdoll()
	{
		if ( !_ragdollGameObject.IsValid() )
		{
			return;
		}

		_ragdollGameObject.Destroy();
		_ragdollGameObject = null;
	}

	private void ClearStunRagdoll()
	{
		if ( !_stunRagdollGameObject.IsValid() )
		{
			return;
		}

		_stunRagdollGameObject.Destroy();
		_stunRagdollGameObject = null;
	}

	public void BeginStunRagdollHost()
	{
		Assert.True( Networking.IsHost );

		if ( IsDead || !HasStatus( Constants.StunStatus ) )
		{
			return;
		}

		_stunRagdollGeneration++;
		var generation = _stunRagdollGeneration;

		ClearStunRagdoll();

		var ragdollGameObject = CreateRagdoll( $"Stun Ragdoll ({DisplayName})" );
		ragdollGameObject.NetworkMode = NetworkMode.Object;
		CopyRagdollClothing( ragdollGameObject );

		ApplyRagdollImpulse( ragdollGameObject, StunRagdollImpulseScale );

		_stunRagdollGameObject = ragdollGameObject;
		_stunRagdollGameObject.WorldPosition = BodyRoot.WorldPosition;
		_stunRagdollGameObject.WorldRotation = BodyRoot.WorldRotation;
		_stunRagdollGameObject.NetworkSpawn();

		SetStunned( true );
		OnStunRagdollRefreshOwner();
		OnStunRagdollStartedBroadcast();

		_ = EndStunRagdollAfterDelay( this, generation );
	}

	public void ApplyStunOwnerEffects()
	{
		Holster();

		if ( EquipmentOverlay.Instance.IsValid() )
		{
			EquipmentOverlay.Instance.IsActive = false;
		}

		Controller.ThirdPerson = true;
	}

	private static async Task EndStunRagdollAfterDelay( Player player, int generation )
	{
		await GameTask.DelaySeconds( Config.Current.Game.StunRagdollDuration );

		if ( !player.IsValid() || !player.HasStatus( Constants.StunStatus ) )
		{
			return;
		}

		if ( player._stunRagdollGeneration != generation )
		{
			return;
		}

		player.ClearStunRagdollHost();
	}

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	private void OnStunRagdollRefreshOwner()
	{
		ApplyStunOwnerEffects();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void OnStunRagdollStartedBroadcast()
	{
		if ( AnimationHelper.IsValid() )
		{
			AnimationHelper.HoldTypePose = 0;
		}
	}

	public void ClearStunRagdollHost()
	{
		Assert.True( Networking.IsHost );

		if ( !_stunRagdollGameObject.IsValid() )
		{
			return;
		}

		var ragdollPosition = GetStunRagdollStandPosition( _stunRagdollGameObject );
		ClearStunRagdoll();

		if ( IsDead )
		{
			return;
		}

		SetStunned( false );
		TeleportHost( new Transform( ragdollPosition, WorldRotation ) );
		DamageTakenForce = Vector3.Zero;

		OnStunRagdollEndedOwner();
		OnStunRagdollEndedBroadcast();
	}

	private Vector3 GetStunRagdollStandPosition( GameObject ragdoll )
	{
		var modelPhysics = ragdoll.GetComponent<ModelPhysics>();
		if ( !modelPhysics.IsValid() || modelPhysics.Bodies.Count == 0 )
		{
			return SnapPositionToGround( ragdoll.WorldPosition, ragdoll );
		}

		var center = Vector3.Zero;
		var lowestPoint = ragdoll.WorldPosition;
		var lowestHeight = float.MaxValue;

		foreach ( var body in modelPhysics.Bodies )
		{
			var worldPosition = body.Component.WorldPosition;
			center += worldPosition;

			if ( worldPosition.z < lowestHeight )
			{
				lowestHeight = worldPosition.z;
				lowestPoint = worldPosition;
			}
		}

		center /= modelPhysics.Bodies.Count;
		var standPoint = new Vector3( center.x, center.y, lowestPoint.z );

		return SnapPositionToGround( standPoint, ragdoll );
	}

	private Vector3 SnapPositionToGround( Vector3 point, GameObject ignore )
	{
		var traceStart = point + Vector3.Up * 64f;
		var trace = Scene.Trace.Ray( traceStart, traceStart + Vector3.Down * 512f )
			.IgnoreGameObjectHierarchy( ignore )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "trigger" )
			.Run();

		return trace.Hit ? trace.HitPosition : point;
	}

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	private void OnStunRagdollEndedOwner()
	{
		UpdatePerspective();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void OnStunRagdollEndedBroadcast()
	{
		if ( AnimationHelper.IsValid() )
		{
			AnimationHelper.HoldTypePose = 3;
		}
	}

	private void ApplyRagdollImpulse( GameObject ragdollGameObject, float scale )
	{
		var modelPhysics = ragdollGameObject.GetComponent<ModelPhysics>();
		if ( !modelPhysics.IsValid() || DamageTakenForce.IsNearZeroLength )
		{
			return;
		}

		foreach ( var body in modelPhysics.Bodies )
		{
			var distance = Vector3.DistanceBetween( body.Component.WorldPosition, DamageTakenPosition );
			var forceMagnitude = Math.Min( 1f - distance / 100f, 1f );
			body.Component.ApplyImpulse( DamageTakenForce * Math.Max( forceMagnitude, 0f ) * scale );
		}
	}

	private void CopyRagdollClothing( GameObject ragdoll )
	{
		if ( !Renderer.IsValid() || GameManager.IsHeadless )
		{
			return;
		}

		var ragdollRenderer = ragdoll.GetComponent<SkinnedModelRenderer>();
		if ( !ragdollRenderer.IsValid() )
		{
			return;
		}

		ragdollRenderer.CopyFrom( Renderer );

		foreach ( var other in Renderer.GameObject.Children.SelectMany( x => x.Components.GetAll<SkinnedModelRenderer>() ) )
		{
			if ( !other.IsValid() )
			{
				continue;
			}

			var attachmentRenderer = new GameObject( true, other.GameObject.Name )
			{
				Parent = ragdoll
			}.Components.Create<SkinnedModelRenderer>();
			attachmentRenderer.CopyFrom( other );
			attachmentRenderer.BoneMergeTarget = ragdollRenderer;
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void SetStunned( bool stunned )
	{
		Controller.Enabled = !stunned && Connection != null && !GameManager.IsHeadless;
		ModelHitboxes.Enabled = !stunned && !GameManager.IsHeadless;
		Renderer.Enabled = !stunned && !GameManager.IsHeadless;

		if ( AnimationHelper.IsValid() )
		{
			AnimationHelper.Enabled = !stunned && !GameManager.IsHeadless;
		}

		GameObject.Tags.Set( "invisible", stunned );

		if ( !stunned )
		{
			ModelHitboxes.Rebuild();
			Transform.ClearInterpolation();
		}
	}

	private void CreateRagdollHost()
	{
		Assert.True( Networking.IsHost );

		if ( _ragdollGameObject.IsValid() )
		{
			return;
		}

		ClearStunRagdoll();

		var ragdollGameObject = CreateRagdoll( $"Ragdoll ({DisplayName})" );
		ragdollGameObject.NetworkMode = NetworkMode.Object;

		var deadPlayer = ragdollGameObject.AddComponent<DeadBody>();
		deadPlayer.Player = this;

		ApplyRagdollImpulse( ragdollGameObject, 1f );

		_ragdollGameObject = ragdollGameObject;
		_ragdollGameObject.WorldPosition = BodyRoot.WorldPosition;
		_ragdollGameObject.WorldRotation = BodyRoot.WorldRotation;

		_ragdollGameObject.NetworkSpawn();
	}

	// Sync the player to ragdoll (for VC/Prox chat)
	private void SyncPlayerRagdoll()
	{
		Assert.True( Networking.IsHost );

		if ( !_ragdollGameObject.IsValid() || _lastRagdollPlayerSync <= PlayerRagdollSyncInterval )
		{
			return;
		}

		_lastRagdollPlayerSync = 0f;

		var distance = Vector3.DistanceBetween( BodyRoot.WorldPosition, _ragdollGameObject.WorldPosition );
		if ( distance > 1f )
		{
			TeleportHost( new Transform( _ragdollGameObject.WorldPosition, Rotation.Identity ) );
		}
	}

	private void SyncStunRagdoll()
	{
		Assert.True( Networking.IsHost );

		if ( !_stunRagdollGameObject.IsValid() || _lastStunRagdollPlayerSync <= PlayerRagdollSyncInterval )
		{
			return;
		}

		_lastStunRagdollPlayerSync = 0f;

		var standPosition = GetStunRagdollStandPosition( _stunRagdollGameObject );
		var distance = Vector3.DistanceBetween( BodyRoot.WorldPosition, standPosition );
		if ( distance > 1f )
		{
			TeleportHost( new Transform( standPosition, WorldRotation ) );
		}
	}
}
