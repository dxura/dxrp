namespace Dxura.RP.Game;

/// <summary>
/// Draws party-member outlines for the local viewer. Delegates to <see cref="PartySystem.Instance"/>.
/// </summary>
public sealed class PartyOutlineSource : IPlayerOutlineSource
{
	public PlayerOutlineRequest? GetOutlineRequest( Player viewer, Player target )
	{
		var party = PartySystem.Instance;
		if ( party is null || !party.Settings.AllowMemberOutline )
		{
			return null;
		}

		foreach ( var kv in party.Parties )
		{
			var members = kv.Value.Members;
			if ( members is null || !members.Contains( viewer.SteamId ) )
			{
				continue;
			}

			if ( !members.Contains( target.SteamId ) || !kv.Value.MemberOutlineEnabled )
			{
				return null;
			}

			var c = kv.Value.Color.ToColor();
			return new PlayerOutlineRequest
			{
				Width = 0.12f,
				Color = c.WithAlpha( 0.12f ),
				ObscuredColor = c.WithAlpha( 0.85f ),
				InsideColor = Color.Transparent,
				InsideObscuredColor = Color.Transparent,
				OverrideTargets = true,
			};
		}

		return null;
	}
}
