using Dxura.RP.Shared;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

/// <summary>
///     An equipment component.
/// </summary>
public class Equipment : Component, IEquipment, IDescription
{
	[Property]
	[Group( "Identity" )]
	public string Identifier { get; set; } = "";

	public GameModeEquipmentDto? Resource => GameModeEquipments.GetByIdentifierOrFallback( Identifier );

	/// <summary>
	///     A tag binder for this equipment.
	/// </summary>
	[RequireComponent]
	public required TagBinder TagBinder { get; set; }

	/// <summary>
	///     The default holdtype for this equipment.
	/// </summary>
	[Property]
	[Group( "Animation" )]
	public AnimationHelper.HoldTypes HoldType { get; set; } = AnimationHelper.HoldTypes.Rifle;

	/// <summary>
	///     The default holdtype for this equipment.
	/// </summary>
	[Property]
	[Group( "Animation" )]
	public AnimationHelper.Hand Handedness { get; set; } = AnimationHelper.Hand.Right;

	/// <summary>
	///     What sound should we play when taking this gun out?
	/// </summary>
	[Property]
	[Group( "Sounds" )]
	public SoundEvent? DeploySound { get; set; }

	[Property]
	[Group( "Prefabs" )]
	public GameObject? ViewModelPrefab { get; set; }

	[Property]
	[Group( "Dropping" )]
	public Vector3 DroppedSize { get; set; } = new( 8, 2, 8 );

	[Property]
	[Group( "Dropping" )]
	public Vector3 DroppedCenter { get; set; }

	[Property]
	[Group( "Damage" )]
	public float? ArmorReduction { get; set; }

	[Property]
	[Group( "Damage" )]
	public float? HelmetReduction { get; set; }

	[Property]
	[Group( "Economy" )]
	public bool IsPurchasable { get; set; } = true;

	/// <summary>
	///     How slower do we walk with this equipment out?
	/// </summary>
	[Property]
	[Group( "Movement" )]
	public float SpeedPenalty { get; set; } = 0f;

	/// <summary>
	///     Should we enable the crosshair?
	/// </summary>
	[Property]
	[Group( "UI" )]
	public bool UseCrosshair { get; set; } = true;

	/// <summary>
	///     Should we enable the simple crosshair?
	/// </summary>
	[Property]
	[Group( "UI" )]
	public bool OnlyShowCrosshairDot { get; set; } = false;

	/// <summary>
	///     Who owns this gun?
	/// </summary>
	public Player? Owner => _owner ??= Scene.Directory.FindComponentByGuid( OwnerId ) as Player;

	/// <summary>
	///     The Guid of the owner's Player
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public Guid OwnerId { get; set; }

	[Sync( SyncFlags.FromHost )]
	public Guid MarketItemId { get; set; }

	/// <summary>
	///     Is this equipment currently deployed by the player?
	/// </summary>
	[Sync]
	[Change( nameof( OnIsDeployedPropertyChanged ) )]
	public bool IsDeployed { get; private set; }

	private bool WasDeployed { get; set; }
	private bool HasStarted { get; set; }

	public bool CanDrop { get; set; }

	/// <summary>
	///     A reference to the equipment's ViewModel if it has one.
	/// </summary>
	public ViewModel? ViewModel
	{
		get => _viewModel;
		set
		{
			_viewModel = value ?? null;

			if ( _viewModel.IsValid() && this.IsValid() )
			{
				_viewModel.Equipment = this;
			}
		}
	}

	private bool HasCreatedViewModel { get; set; }

	/// <summary>
	///     Cached version of the owner once we fetch it.
	/// </summary>
	private Player? _owner;

	private ViewModel? _viewModel;
	string IDescription.Icon => Resource.DisplayIcon();

	// IDescription
	string IDescription.DisplayName => Resource.DisplayName();

	/// <summary>
	///     A reference to the equipment's model renderer.
	/// </summary>
	[Property]
	[Group( "Components" )]
	public required SkinnedModelRenderer ModelRenderer { get; set; }

	[Property] [Group( "GameObjects" )] public GameObject? Muzzle { get; set; }
	[Property] [Group( "GameObjects" )] public GameObject? EjectionPort { get; set; }

	/// <summary>
	///     Shorthand to bind a tag.
	/// </summary>
	/// <param name="tag"></param>
	/// <param name="predicate"></param>
	internal void BindTag( string tag, Func<bool> predicate )
	{
		TagBinder.BindTag( tag, predicate );
	}

	/// <summary>
	///     Updates the render mode, if we're locally controlling a player, we want to hide the world model.
	/// </summary>
	public void UpdateRenderMode( bool force = false )
	{
		if ( !ModelRenderer.IsValid() )
		{
			return;
		}

		if ( GameManager.IsHeadless )
		{
			ModelRenderer.Enabled = false;
			return;
		}

		var on = force || Owner.IsValid() && (!Owner.IsLocalPlayer || Owner.Controller.ThirdPerson) && IsDeployed;

		if ( !Owner.IsValid() && !force )
		{
			on = false;
		}

		ModelRenderer.Enabled = on;
		ModelRenderer.RenderType = on
			? Sandbox.ModelRenderer.ShadowRenderType.On
			: Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
	}

	/// <summary>
	///     Deploy this equipment.
	/// </summary>
	public void Deploy()
	{
		if ( !IsValid )
		{
			return;
		}

		Assert.True( !IsProxy );

		if ( IsDeployed )
		{
			return;
		}

		// We must first holster all other equipment items.
		if ( Owner.IsValid() )
		{
			var equipment = Owner.Equipment.ToList();

			foreach ( var item in equipment )
			{
				item.Holster();
			}
		}

		IsDeployed = true;
	}

	/// <summary>
	///     Holster this equipment.
	/// </summary>
	public void Holster()
	{
		if ( !IsValid || IsProxy )
		{
			return;
		}

		if ( !IsDeployed )
		{
			return;
		}

		IsDeployed = false;
	}

	/// <summary>
	///     Allow equipment to override holdtypes at any notice.
	/// </summary>
	/// <returns></returns>
	public virtual AnimationHelper.HoldTypes GetHoldType()
	{
		return HoldType;
	}

	private void OnIsDeployedPropertyChanged( bool oldValue, bool newValue )
	{
		if ( !HasStarted )
		{
			return;
		}

		UpdateDeployedState();
	}

	private void UpdateDeployedState()
	{
		if ( IsDeployed == WasDeployed )
		{
			return;
		}

		switch ( WasDeployed )
		{
			case false when IsDeployed:
				OnDeployed();
				break;
			case true when !IsDeployed:
				OnHolstered();
				break;
		}

		WasDeployed = IsDeployed;
	}

	public void ClearViewModel()
	{
		if ( ViewModel.IsValid() && ViewModel.GameObject.IsValid() )
		{
			ViewModel.GameObject.Destroy();
		}
	}

	/// <summary>
	///     Creates a viewmodel for the player to use.
	/// </summary>
	public void CreateViewModel( bool playDeployEffects = true )
	{
		var player = Owner;
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !Resource.IsValid() )
		{
			return;
		}

		ClearViewModel();
		UpdateRenderMode();

		var viewModelPrefab = ViewModelPrefab;
		var secondaryPrefabPath = Resource?.SecondaryPrefabPath();
		if ( !viewModelPrefab.IsValid() && !string.IsNullOrWhiteSpace( secondaryPrefabPath ) )
		{
			viewModelPrefab = GameObject.GetPrefab( secondaryPrefabPath );
		}

		if ( viewModelPrefab.IsValid() )
		{
			var viewModelGameObject = viewModelPrefab.Clone( new CloneConfig
			{
				Transform = global::Transform.Zero, Parent = player.ViewModelGameObject, StartEnabled = true
			} );


			var viewModelComponent = viewModelGameObject.Components.Get<ViewModel>();
			viewModelComponent.PlayDeployEffects = playDeployEffects;

			// equipment needs to know about the ViewModel
			ViewModel = viewModelComponent;

			viewModelGameObject.BreakFromPrefab();
		}

		if ( !playDeployEffects )
		{
			return;
		}

		if ( DeploySound is null )
		{
			return;
		}

		DeploySound.Play( Transform.World.Position );
	}

	protected override void OnStart()
	{
		WasDeployed = IsDeployed;
		HasStarted = true;

		if ( IsDeployed )
		{
			OnDeployed();
		}
		else
		{
			OnHolstered();
		}
	}

	protected virtual void OnDeployed()
	{
		if ( Owner.IsValid() && Owner is { IsLocalPlayer: true, Controller.ThirdPerson: false } )
		{
			CreateViewModel( !HasCreatedViewModel );
			HasCreatedViewModel = true;
		}


		UpdateRenderMode();

		IEquipmentEvents.PostToGameObject( GameObject.Root, x => x.OnEquipmentDeployed( this ) );
	}

	protected virtual void OnHolstered()
	{
		UpdateRenderMode();
		ClearViewModel();

		IEquipmentEvents.PostToGameObject( GameObject.Root, x => x.OnEquipmentHolstered( this ) );
	}

	protected override void OnDestroy()
	{
		ClearViewModel();

		IEquipmentEvents.PostToGameObject( GameObject.Root, x => x.OnEquipmentDestroyed( this ) );
	}
}
