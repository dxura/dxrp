namespace Dxura.RP.Game;

/// <summary>
/// Draws a red through-wall outline on the local player's last killer while they are dead.
/// </summary>
public sealed class PlayerDeathOutlineSource : IPlayerOutlineSource
{

	public PlayerOutlineRequest? GetOutlineRequest( Player viewer, Player target )
	{
		if ( viewer.HealthComponent.State != LifeState.Dead )
		{
			return null;
		}

		if ( viewer.GetLastKiller() != target )
		{
			return null;
		}

		return new PlayerOutlineRequest
		{
			Width = 0.1f,
			Color = Color.Transparent,
			ObscuredColor = Color.Red,
			InsideColor = target.HealthComponent.IsGodMode ? Color.White.WithAlpha( 0.1f ) : Color.Transparent,
			InsideObscuredColor = Color.Transparent,
			OverrideTargets = false,
		};
	}
}
