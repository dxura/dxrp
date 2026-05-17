using Dxura.RP.Game.Tools;
using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Equipments;

public class ToolEquipment : InputWeaponComponent, IEquipmentEvents
{
	[Property] [Group( "Sounds" )] private SoundEvent? UseSound { get; set; }

	[Property] [Group( "Prefabs" )] public required GameObject LinePrefab { get; set; }

	public BaseTool? CurrentTool;

	public new void OnEquipmentDeployed( Equipment equipment )
	{
		if ( IsProxy )
		{
			return;
		}

		if ( CurrentTool == null )
		{
			SetTool( TypeLibrary.GetType<BaseTool>( "Dxura.RP.Game.Tools.TextTool" ) );
		}
	}

	protected override void OnInputUpdate()
	{
		CurrentTool?.OnToolUpdate();
	}

	protected override void OnInputFixedUpdate()
	{
		CurrentTool?.OnToolFixedUpdate();

		if ( Input.Pressed( "attack1" ) )
		{
			CurrentTool?.PrimaryUseStart();
		}

		if ( Input.Down( "attack1" ) )
		{
			CurrentTool?.PrimaryUseUpdate();
		}

		if ( Input.Released( "attack1" ) )
		{
			CurrentTool?.PrimaryUseEnd();
		}

		if ( Input.Pressed( "attack2" ) )
		{
			CurrentTool?.SecondaryUseStart();
		}

		if ( Input.Down( "attack2" ) )
		{
			CurrentTool?.SecondaryUseUpdate();
		}

		if ( Input.Released( "attack2" ) )
		{
			CurrentTool?.SecondaryUseEnd();
		}

		if ( Input.Pressed( "reload" ) )
		{
			CurrentTool?.ReloadUseStart();
		}

		if ( Input.Down( "reload" ) )
		{
			CurrentTool?.ReloadUseUpdate();
		}

		if ( Input.Released( "reload" ) )
		{
			CurrentTool?.ReloadUseEnd();
		}
	}

	public void SetTool( TypeDescription? toolDescription )
	{
		if ( CurrentTool != null )
		{
			if ( CurrentTool.GetType() == toolDescription?.TargetType )
			{
				return;
			}

			CurrentTool?.OnUnequip();
		}

		if ( toolDescription == null )
		{
			CurrentTool = null;
			return;
		}

		var newTool = TypeLibrary.Create<BaseTool>( toolDescription.TargetType );
		newTool.Tool = this;
		CurrentTool = newTool;
		newTool.OnEquip();

		ToolMenu.Instance?.UpdateInspector();
	}

	public new void OnEquipmentHolstered( Equipment equipment )
	{
		CurrentTool?.OnUnequip();
	}

	public void OnEquipmentDestroyed( Equipment equipment )
	{
		CurrentTool?.OnUnequip();
	}

	protected override void OnDisabled()
	{
		CurrentTool?.OnUnequip();
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Unreliable )]
	public void BroadcastUseEffectsHost( Vector3 hitPosition, Vector3 hitNormal = default )
	{
		var caller = Rpc.Caller;
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:tool:effects", Config.Current.Game.ToolEffectsCooldown ) )
		{
			return;
		}

		using ( Rpc.FilterExclude( caller ) )
		{
			BroadcastUseEffects( hitPosition, hitNormal );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void BroadcastUseEffects( Vector3 hitPosition, Vector3 hitNormal = default )
	{
		DoUseEffects( false, hitPosition, hitNormal );
	}

	public void DoUseEffects( bool broadcast, Vector3 hitPosition, Vector3 hitNormal = default )
	{
		if ( broadcast )
		{
			BroadcastUseEffectsHost( hitPosition, hitNormal );
		}

		Equipment.Owner?.Renderer.Set( "b_attack", true );
		Equipment.ViewModel?.ModelRenderer.Set( "b_attack", true );

		UseSound.Play( WorldPosition );
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	public void BroadcastGrabbedHost( GameObject target, bool isGrab, bool ownerPhysics = true )
	{
		var caller = Rpc.Caller;

		if ( !target.IsValid() )
		{
			return;
		}
		if ( !GameUtils.HasPermission( caller, target ) )
		{
			return;
		}

		if ( isGrab )
		{
			GameManager.Instance.BroadcastTagHost( target, true, Constants.GrabbedTag );
		}
		else
		{
			GameManager.Instance.BroadcastTagHost( target, false, Constants.GrabbedTag );

			var guard = target.GetOrAddComponent<CollideGuard>();
			guard.ResetTimer();
		}
	}
}
