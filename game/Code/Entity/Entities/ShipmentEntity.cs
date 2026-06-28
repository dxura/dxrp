using Dxura.RP.Game.Equipments;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game.Entities;

public class ShipmentEntity : BaseEntity, IWireUsable, Component.IPressable
{
	[Property]
	[ReadOnly]
	[Sync( SyncFlags.FromHost )]
	public Guid MarketItemId { get; set; }

	[Property]
	[Change( nameof( OnQuantityChange ) )]
	[Sync( SyncFlags.FromHost )]
	public int Quantity { get; private set; }

	[Property]
	public Guid EquipmentId { get; set; }

	[Property]
	[Sync( SyncFlags.FromHost )]
	public int MaxQuantity { get; set; } = 10;

	[Property] public required GameObject EquipmentPreview { get; set; }
	[Property] public required ModelRenderer EquipmentRenderer { get; set; }
	[Property] public required TextRenderer TypeText { get; set; }
	[Property] public required TextRenderer QuantityText { get; set; }

	private readonly HashSet<GameObject> _depositBlockedUntilExit = new();

	private BoxCollider? _collider;
	private float _totalAnimationTime;
	private Vector3 _originalPreviewPosition;
	private bool _previewPositionSaved;
	private bool _occluded;

	public override string DisplayName
	{
		get
		{
			var name = GameModeEntity.DisplayName();
			if ( name.StartsWith( '#' ) )
			{
				name = Language.GetPhrase( name[1..] );
			}

			return $"{name} ({QuantityText.Text})";
		}
	}

	protected override void OnStart()
	{
		base.OnStart();

		_collider = GameObject.Components.GetAll<BoxCollider>( FindMode.EverythingInSelf )
			.FirstOrDefault( collider => collider.IsValid() && !collider.IsTrigger );

		UpdateState();

		_originalPreviewPosition = EquipmentPreview.LocalPosition;
		_previewPositionSaved = true;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( !Networking.IsHost || !_collider.IsValid() )
		{
			return;
		}

		ProcessDeposits();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !_occluded && _previewPositionSaved && !GameManager.IsHeadless )
		{
			AnimatePreview();
		}
	}

	public override void OnOcclusionChanged( bool occlude )
	{
		base.OnOcclusionChanged( occlude );
		_occluded = occlude;
	}

	protected override void OnDestroyed()
	{
		Assert.True( Networking.IsHost );

		var equipment = GetEquipment();
		if ( equipment != null )
		{
			for ( var i = 0; i < Quantity; i++ )
			{
				DropEquipmentHost( equipment );
			}
		}

		base.OnDestroyed();
	}

	public void ConfigureHost( GameModeEquipmentDto equipment, int quantity )
	{
		Assert.True( Networking.IsHost );

		EquipmentId = equipment.GameModeAddonContentId;
		MaxQuantity = Math.Max( 1, quantity );
		Quantity = MaxQuantity;
		UpdateState();
	}

	private void ProcessDeposits()
	{
		var bounds = _collider!.GetWorldBounds().Grow( 4f );
		var insideRoots = new HashSet<GameObject>();

		foreach ( var gameObject in Scene.FindInPhysics( bounds ) )
		{
			if ( gameObject.Root.IsValid() )
			{
				insideRoots.Add( gameObject.Root );
			}
		}

		_depositBlockedUntilExit.RemoveWhere( go => !go.IsValid() || !insideRoots.Contains( go ) );

		if ( Quantity >= MaxQuantity )
		{
			return;
		}

		foreach ( var root in insideRoots )
		{
			var droppedEquipment = root.GetComponent<DroppedEquipment>();
			if ( droppedEquipment.IsValid() )
			{
				TryDeposit( droppedEquipment );
			}
		}
	}

	public bool Press( IPressable.Event e )
	{
		var hands = Player.Local.GetComponentInChildren<HandsEquipment>();
		if ( hands.IsValid() && hands.IsHolding( GameObject, true ) )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "shipment:use", Config.Current.Game.ShipmentUseCooldown, true ) )
		{
			return false;
		}

		UseHost();
		return true;
	}

	public void OnWireUse( long owner, Vector3 userPosition )
	{
		InternalUse();
	}

	[Rpc.Host]
	private void UseHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:shipment:use", Config.Current.Game.ShipmentUseCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		var tr = Scene.Trace.Ray( player.AimRay, Config.Current.Game.ReachDistance )
			.IgnoreGameObjectHierarchy( player.GameObject )
			.UseHitboxes()
			.Run();

		if ( !tr.Hit || tr.GameObject.Root != GameObject.Root )
		{
			return;
		}

		InternalUse();
	}

	private void InternalUse()
	{
		var equipment = GetEquipment();
		if ( equipment == null )
		{
			return;
		}

		Quantity--;
		DropEquipmentHost( equipment );

		if ( Quantity == 0 )
		{
			GameObject.Destroy();
		}
	}

	private DroppedEquipment DropEquipmentHost( GameModeEquipmentDto equipment )
	{
		var dropped = DroppedEquipment.CreateHost(
			equipment,
			EquipmentPreview.WorldPosition,
			EquipmentPreview.WorldRotation,
			marketItemId: MarketItemId );

		BlockDepositUntilExit( dropped.GameObject );
		return dropped;
	}

	private bool TryDeposit( DroppedEquipment droppedEquipment )
	{
		if ( Quantity >= MaxQuantity || droppedEquipment.Resource == null )
		{
			return false;
		}

		if ( IsDepositBlocked( droppedEquipment.GameObject ) || !MatchesEquipment( droppedEquipment ) )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{GameObject.Id}:shipment:deposit", Config.Current.Game.ShipmentUseCooldown ) )
		{
			return false;
		}

		Quantity++;
		droppedEquipment.GameObject.Destroy();
		return true;
	}

	private void BlockDepositUntilExit( GameObject droppedObject )
	{
		_depositBlockedUntilExit.Add( droppedObject );
	}

	private bool IsDepositBlocked( GameObject droppedObject )
	{
		return _depositBlockedUntilExit.Contains( droppedObject );
	}

	private bool MatchesEquipment( DroppedEquipment droppedEquipment )
	{
		return string.Equals( droppedEquipment.Identifier, EquipmentIdentifier, StringComparison.OrdinalIgnoreCase );
	}

	private GameModeEquipmentDto? GetEquipment()
	{
		return GameModeEquipments.FindByIdentifier( EquipmentIdentifier );
	}

	private void UpdateState()
	{
		if ( !EquipmentRenderer.IsValid() || !TypeText.IsValid() || !QuantityText.IsValid() )
		{
			return;
		}

		if ( Networking.IsHost && Quantity <= 0 )
		{
			Quantity = MaxQuantity;
		}

		var equipment = GetEquipment();
		EquipmentRenderer.Model = equipment.GetWorldModel();
		EquipmentRenderer.WorldScale = 1.1f;
		TypeText.Text = equipment.DisplayName();
		QuantityText.Text = $"{Quantity}/{MaxQuantity}";
	}

	private void OnQuantityChange( int oldValue, int newValue )
	{
		QuantityText.Text = $"{Quantity}/{MaxQuantity}";
	}

	private void AnimatePreview()
	{
		_totalAnimationTime = (_totalAnimationTime + Time.Delta) % 360f;

		const float bobHeight = 2f;
		const float bobSpeed = 2.0f;
		const float rotationSpeed = 45.0f;

		var verticalOffset = MathF.Sin( _totalAnimationTime * bobSpeed ) * bobHeight;

		EquipmentPreview.LocalPosition = new Vector3(
			_originalPreviewPosition.x,
			_originalPreviewPosition.y,
			_originalPreviewPosition.z + verticalOffset
		);

		EquipmentPreview.LocalRotation = Rotation.FromYaw( _totalAnimationTime * rotationSpeed );
	}
}
