using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public partial class Player
{
	private const float PlayerRagdollSyncInterval = 0.5f;
	private TimeSince _lastRagdollPlayerSync = 0f;
	private GameObject? _ragdollGameObject;

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

	private void CreateRagdollHost()
	{
		Assert.True( Networking.IsHost );

		if ( _ragdollGameObject.IsValid() )
		{
			return;
		}

		var ragdollGameObject = CreateRagdoll( $"Ragdoll ({DisplayName})" );
		ragdollGameObject.NetworkMode = NetworkMode.Object;

		var deadPlayer = ragdollGameObject.AddComponent<DeadBody>();
		deadPlayer.Player = this;

		var modelPhysics = ragdollGameObject.GetComponent<ModelPhysics>();
		if ( modelPhysics.IsValid() )
		{
			foreach ( var body in modelPhysics.Bodies )
			{
				var distance = Vector3.DistanceBetween( body.Component.WorldPosition, DamageTakenPosition );
				var forceMagnitude = Math.Min( 1f - distance / 100f, 1f );
				body.Component.ApplyImpulse( DamageTakenForce * Math.Max( forceMagnitude, 0f ) );
			}
		}

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
}
