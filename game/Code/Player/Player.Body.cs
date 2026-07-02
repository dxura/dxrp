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

	private int _clothingApplyVersion;

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
	}

	private void ResetBody()
	{
		DamageTakenForce = Vector3.Zero;
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
		if ( !Renderer.IsValid() || !Job.IsValid() || GameManager.IsHeadless )
		{
			return;
		}

		var applyVersion = ++_clothingApplyVersion;

		if ( !Job.SupportsCitizenClothing() )
		{
			if ( Dresser.IsValid() )
			{
				Dresser.Clothing.Clear();
				Dresser.Enabled = false;
			}

			CleanupDresserClothing();

			if ( !Job.HasCloudModel() )
			{
				ApplyUndressedModel( Job.GetPrimaryModel() );
				return;
			}

			_ = ApplyUndressedModelAsync( applyVersion );
			return;
		}

		if ( !Dresser.IsValid() )
		{
			return;
		}

		Dresser.Enabled = true;

		// 1: Apply model
		Renderer.Model = Job.GetPrimaryModel();

		// 2: Build and apply citizen clothing
		var includeJobClothing = DxCivilianJobClothing || !Job.IsInGroup( "0802e49f-43ba-5bf2-adb0-933b150f0156" );
		Dresser.Clothing.Clear();
		Dresser.Clothing.AddRange( Job.BuildClothing( this, includeJobClothing ).Clothing );

		_ = ApplyClothingAsync( applyVersion );
	}

	private async Task ApplyClothingAsync( int applyVersion )
	{
		// Wait for a fixed update so stuff is ready
		await Task.FixedUpdate();

		if ( !GameObject.IsValid() || !Dresser.IsValid() || applyVersion != _clothingApplyVersion )
		{
			return;
		}

		// Resolve cloud model (may need to download/mount the package)
		var model = await Job.GetPrimaryModelAsync();

		if ( !GameObject.IsValid() || !Dresser.IsValid() || applyVersion != _clothingApplyVersion )
		{
			return;
		}

		if ( Renderer.IsValid() )
		{
			Renderer.Model = model;
		}

		await Dresser.Apply();

		if ( !GameObject.IsValid() )
		{
			return;
		}

		if ( applyVersion != _clothingApplyVersion )
		{
			if ( !Job.SupportsCitizenClothing() )
			{
				CleanupDresserClothing();
			}

			return;
		}

		if ( !ModelHitboxes.IsValid() )
		{
			return;
		}

		ModelHitboxes.Rebuild();
	}

	private async Task ApplyUndressedModelAsync( int applyVersion )
	{
		await Task.FixedUpdate();

		if ( !GameObject.IsValid() || applyVersion != _clothingApplyVersion )
		{
			return;
		}

		var model = await Job.GetPrimaryModelAsync();

		if ( !GameObject.IsValid() || applyVersion != _clothingApplyVersion )
		{
			return;
		}

		ApplyUndressedModel( model );
	}

	private void ApplyUndressedModel( Model model )
	{
		if ( Dresser.IsValid() )
		{
			Dresser.Clothing.Clear();
			Dresser.Enabled = false;
		}

		CleanupDresserClothing();

		if ( Renderer.IsValid() )
		{
			Renderer.Model = model;
		}

		if ( ModelHitboxes.IsValid() )
		{
			ModelHitboxes.Rebuild();
		}
	}

	private void CleanupDresserClothing()
	{
		if ( !BodyRoot.IsValid() )
		{
			return;
		}

		foreach ( var skinnedRenderer in BodyRoot.GetComponentsInChildren<SkinnedModelRenderer>( true ).ToArray() )
		{
			if ( skinnedRenderer == Renderer || skinnedRenderer == EmoteRenderer )
			{
				continue;
			}

			if ( !skinnedRenderer.GameObject.Name.StartsWith( "Clothing - ", StringComparison.Ordinal ) )
			{
				continue;
			}

			skinnedRenderer.GameObject.Destroy();
		}
	}

	public void ClearClothing()
	{
		if ( !Renderer.IsValid() )
		{
			return;
		}

		CleanupDresserClothing();

		if ( !Job.IsValid() || !Job.SupportsCitizenClothing() )
		{
			Renderer.Enabled = false;
			return;
		}

		var container = new ClothingContainer();
		container.Apply( Renderer );
		Renderer.Enabled = false;
	}

	private void OnDestroyBody()
	{
		ClearRagdoll();
	}

	[ConVar( "dx_civilian_job_clothing", ConVarFlags.Saved )]
	private static bool DxCivilianJobClothing { get; set; } = true;
}
