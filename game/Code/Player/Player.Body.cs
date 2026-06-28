using System.Threading.Tasks;

namespace Dxura.RP.Game;

public partial class Player
{
	[Property] [Feature( "Body" )] public required GameObject BodyRoot { get; set; }
	[Property] [Feature( "Body" )] public required GameObject HatRoot { get; set; }

	[Property] [Feature( "Body" )]
	public required GameObject BodyForward { get; set; }
	[Property] [Feature( "Body" )] public required SkinnedModelRenderer Renderer { get; set; }
	[Property] [Feature( "Body" )] public required Rigidbody Rigidbody { get; set; }

	[Property] [Feature( "Body" )] public required ModelHitboxes ModelHitboxes { get; set; }
	[Property] [Feature( "Body" )] public required ModelPhysics ModelPhysics { get; set; }
	[Property] [Feature( "Body" )] public required GameObject NamePlate { get; set; }
	[Property] [Feature( "Body" )] public required Dresser Dresser { get; set; }
	[Property] [Feature( "Body" )] public GameObject? ChestBone { get; set; }
	[Property] [Feature( "Body" )] public SkinnedModelRenderer? EmoteRenderer { get; set; }

	[Sync]
	[Property] [Feature( "Body" )] public bool IsTyping { get; set; } = false;
	[Property] [Feature( "Body" )] public required GameObject TypingHandTarget { get; set; }

	/// <summary>
	///     A reference to the animation helper (normally on the Body GameObject)
	/// </summary>
	[Property]
	[Feature( "Body" )]
	public AnimationHelper? AnimationHelper { get; set; }

	public Vector3 DamageTakenPosition { get; set; }
	public Vector3 DamageTakenForce { get; set; }

	private void OnStartBody()
	{
		NamePlate.Enabled = !IsLocalPlayer && !GameManager.IsHeadless;

		Dresser.Enabled = !GameManager.IsHeadless;
		Controller.Enabled = !GameManager.IsHeadless && Connection != null;
		Renderer.Enabled = !GameManager.IsHeadless;
		ModelHitboxes.Enabled = !GameManager.IsHeadless;
		Rigidbody.Enabled = !GameManager.IsHeadless;

		if ( AnimationHelper.IsValid() )
		{
			AnimationHelper.Enabled = !GameManager.IsHeadless;
		}
	}

	/// <summary>
	///     Handles body animations (that's seen by other players)
	/// </summary>
	private void OnUpdateBody()
	{
		if ( !AnimationHelper.IsValid() )
		{
			return;
		}

		switch ( IsTyping )
		{
			case true when !AnimationHelper.IkLeftHand.IsValid():
				AnimationHelper.IkLeftHand = TypingHandTarget;
				return;
			case false when AnimationHelper.IkLeftHand == TypingHandTarget:
				AnimationHelper.IkLeftHand = null;
				break;
		}

		var holdType = AnimationHelper.HoldTypes.None;
		var handiness = AnimationHelper.Hand.Both;

		if ( CurrentEquipment.IsValid() )
		{
			holdType = CurrentEquipment.GetHoldType();
			handiness = CurrentEquipment?.Handedness ?? AnimationHelper.Hand.Both;
		}

		AnimationHelper.Handedness = handiness;
		AnimationHelper.HoldType = holdType;

		if ( Networking.IsHost && HealthComponent.State == LifeState.Dead )
		{
			SyncPlayerRagdoll();
		}
		else if ( Networking.IsHost && HasStatus( Constants.StunStatus ) )
		{
			SyncStunRagdoll();
		}
	}

	private void ResetBody()
	{
		DamageTakenForce = Vector3.Zero;
		ClearStunRagdoll();
		SetStunned( false );
		ClearRagdoll();
		SetDead( false );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void SetDead( bool dead )
	{
		Controller.Enabled = !dead && Connection != null && !GameManager.IsHeadless;
		ModelHitboxes.Enabled = !dead && !GameManager.IsHeadless;

		GameObject.Tags.Set( "invisible", dead );
		GameObject.Tags.Set( "playerclip", dead );

		if ( !dead )
		{
			ModelHitboxes.Rebuild();
		}

		Transform.ClearInterpolation();
	}


	/// <summary>
	///     Called to wear an outfit based off current job.
	/// </summary>
	public void ApplyClothing()
	{
		if ( !Renderer.IsValid() || !Job.IsValid() || !Dresser.IsValid() || GameManager.IsHeadless )
		{
			return;
		}

		// 1: Apply model
		Renderer.Model = Job.GetPrimaryModel();

		// 2: Build and apply clothing
		var includeJobClothing = DxCivilianJobClothing || !Job.IsInGroup( "0802e49f-43ba-5bf2-adb0-933b150f0156" );
		Dresser.Clothing.Clear();
		Dresser.Clothing.AddRange( Job.BuildClothing( this, includeJobClothing ).Clothing );

		_ = ApplyClothingAsync();
	}

	public async Task ApplyClothingAsync()
	{
		// Wait for a fixed update so stuff is ready
		await Task.FixedUpdate();

		if ( !GameObject.IsValid() || !Dresser.IsValid() )
		{
			return;
		}

		// Resolve cloud model (may need to download/mount the package)
		var model = await Job.GetPrimaryModelAsync();

		if ( Renderer.IsValid() )
		{
			Renderer.Model = model;
		}

		await Dresser.Apply();

		if ( !GameObject.IsValid() || !ModelHitboxes.IsValid() )
		{
			return;
		}

		ModelHitboxes.Rebuild();
	}

	public void ClearClothing()
	{
		if ( !Renderer.IsValid() )
		{
			return;
		}

		var container = new ClothingContainer();
		container.Apply( Renderer );
		Renderer.Enabled = false;
	}

	private void OnDestroyBody()
	{
		ClearStunRagdoll();
		ClearRagdoll();
	}

	[ConVar( "dx_civilian_job_clothing", ConVarFlags.Saved )]
	private static bool DxCivilianJobClothing { get; set; } = true;
}
