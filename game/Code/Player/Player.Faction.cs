namespace Dxura.RP.Game;

public partial class Player
{
	[Sync( SyncFlags.FromHost )] [Property] [Group( "Faction" )]
	public Guid? FactionId { get; set; }

	[Sync( SyncFlags.FromHost )] [Property] [Group( "Faction" )]
	public Guid? FactionRoleId { get; set; }

	public bool IsInFaction => FactionId.HasValue;

	public FactionInfo? GetFaction()
	{
		if ( !FactionId.HasValue || !FactionSystem.Instance.IsValid() )
		{
			return null;
		}

		return FactionSystem.Instance.Factions.TryGetValue( FactionId.Value, out var faction ) ? faction : null;
	}

	public FactionRoleInfo? GetFactionRole()
	{
		if ( !FactionRoleId.HasValue || !FactionSystem.Instance.IsValid() )
		{
			return null;
		}

		return FactionSystem.Instance.FactionRoles.TryGetValue( FactionRoleId.Value, out var role ) ? role : null;
	}
}
