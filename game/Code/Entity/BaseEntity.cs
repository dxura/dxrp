using Dxura.RP.Shared;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

/// <summary>
///     Represents a generic base component that provides common functionality
///     for various types of interactable entities such as dropped money, printers, food, etc.
/// </summary>
public class BaseEntity : Component, IDamageEvents, IDescription, IOwned, IGameObjectNetworkEvents, ISnapshot,  IOcclusionEvents
{
	// The owner of the entity (Player's ID)
	[Property] [ReadOnly]
	[Sync( SyncFlags.FromHost )]
	public long Owner { get; set; }

	[Property]
	[ReadOnly]
	[Sync( SyncFlags.FromHost )]
	public Guid EntityId { get; private set; }

	public GameModeEntityDto? GameModeEntity => GameModeEntities.FindById( EntityId );
	public GameModeAddonContentDto? Content => GameModeEntity.Content();
	
	public bool DestroyOnDisconnect => GameModeEntity?.DestroyOnDisconnect ?? true;
	public virtual bool DestroyOnJobChange => GameModeEntity?.DestroyOnJobChange ?? true;
	public virtual bool AllowOwnershipTransfer => GameModeEntity?.AllowOwnershipTransfer ?? false;

	/// <summary>
	///     Health component (If we have one)
	/// </summary>
	[Property]
	public HealthComponent? HealthComponent { get; set; }

	[Property]
	public Rigidbody? Rigidbody { get; set; }

	public virtual string? DisplayName => EntityId != Guid.Empty
		? GameModeEntity.DisplayName()
		: GameObject.Name;
	
	public virtual Color Color => Color.White;

	protected override void OnStart()
	{
		base.OnStart();

		if ( Networking.IsHost && EntityId != Guid.Empty )
		{
			var entityToApply = GameModeEntities.FindById( EntityId );
			if ( entityToApply != null )
			{
				ApplyGameModeEntityHostSettings( entityToApply );
			}
		}

		if ( Rigidbody.IsValid() )
		{
			Rigidbody.RigidbodyFlags |= RigidbodyFlags.DisableCollisionSounds;
		}
	}

	public virtual void OnOcclusionChanged( bool occlude )
	{
		GameObject.Network.Interpolation = !occlude;
	}

	public void OnKillHost( Component victim, DamageInfo damage )
	{
		_ = victim;
		_ = damage;
		OnDestroyed();
	}

	/// <summary>
	///     Called when the entity's health reaches zero. Destroy it
	/// </summary>
	protected virtual void OnDestroyed()
	{
		if ( !GameObject.IsValid() )
		{
			return;
		}
		GameObject.Destroy();
	}

	// Don't simulate physics on the host, as it will cause performance issues
	public void NetworkOwnerChanged( Connection? newOwner, Connection? previousOwner )
	{
		if ( newOwner != null || !Networking.IsHost )
		{
			return;
		}

		var rb = GameObject.GetComponent<Rigidbody>();

		if ( !rb.IsValid() )
		{
			return;
		}

		rb.MotionEnabled = false;
		rb.Velocity = Vector3.Zero;
		rb.AngularVelocity = Vector3.Zero;
	}

	/// <summary>
	/// Check if the given player has permission to scale this entity
	/// </summary>
	/// <param name="player">The player attempting to scale</param>
	/// <returns>True if the player can scale this entity</returns>
	public virtual bool CanScale( Player player )
	{
		return false;
	}

	public void ConfigureGameModeEntityHost( GameModeEntityDto entity )
	{
		Assert.True( Networking.IsHost );

		EntityId = entity.GameModeAddonContentId;
		ApplyGameModeEntityHostSettings( entity );
	}

	public TConfig GetConfig<TConfig>() where TConfig : class, new()
	{
		return Config.Current.Content<TConfig>( Content?.Id ?? Guid.Empty );
	}

	public TConfig GetConfig<TConfig>( TConfig fallback ) where TConfig : class, new()
	{
		return Config.Current.Content( Content?.Id ?? Guid.Empty, fallback );
	}

	private void ApplyGameModeEntityHostSettings( GameModeEntityDto entity )
	{
		if ( !entity.HealthEnabled )
		{
			if ( HealthComponent.IsValid() )
			{
				HealthComponent.Destroy();
			}

			HealthComponent = null;
			return;
		}

		var healthComponent = GameObject.GetComponent<HealthComponent>() ?? GameObject.Components.Create<HealthComponent>();
		healthComponent.MaxHealth = entity.HealthAmount;
		healthComponent.Health = entity.HealthAmount;
		healthComponent.State = LifeState.Alive;
		HealthComponent = healthComponent;
	}

	/// <summary>
	/// Apply scaling to this entity with proper ownership handling
	/// </summary>
	/// <param name="scaleValues">The scale values to apply</param>
	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	public void ApplyScaleOwner( Vector3 scaleValues )
	{
		GameObject.WorldScale = scaleValues;
	}

	public SnapshotType SnapshotType => SnapshotType.GameObject;
}
