namespace Dxura.RP.Game;

public abstract class BaseStatus : IStatus
{
	public abstract string Id { get; }
	public abstract string Name { get; }
	public virtual string LongName => Name;
	public virtual string? Icon => null;
	public virtual string? MaterialIcon => null;

	public virtual Color Color => Color.White;

	public TimeUntil? Expiry { get; set; }

	// Duration
	public virtual float? DefaultDuration => null;
	public virtual bool RemoveOnDeath => false;
	public virtual bool RemoveOnRespawn => false;
	public virtual bool RemoveOnJobChange => false;
	public virtual bool RemoveOnArrest => false;

	// Display
	public virtual bool Visible => true;
	public virtual bool ShowText => true;
	public virtual bool ShowOnNameplate => false;
	public virtual bool ShowOnPlayerList => false;

	// Stacks
	public virtual bool Stackable => false;
	public int CurrentStacks { get; set; } = 1;
	public virtual int MaxStacks => 1;

	// Modifiers
	public virtual bool PreventFallDamage => false;

	// Lifecycle

	public virtual void OnAddedServer( Player player ) {}
	public virtual void OnAddedOwner( Player player ) {}
	public virtual void OnAddedBroadcast( Player player ) {}

	public virtual void OnRemovedServer( Player player ) {}
	public virtual void OnRemovedOwner( Player player ) {}
	public virtual void OnRemovedBroadcast( Player player ) {}

	public virtual void OnSecondlyUpdateServer( Player player ) {}
	public virtual void OnUpdateOwner( Player player ) {}

	public virtual string ModifyChat( Player player, string message, MessageType messageType ) => message;

	public virtual float ModifyDamageTaken( Player player ) => 1f;
}
