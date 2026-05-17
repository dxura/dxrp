namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	// Mystery Box Entity
	public virtual float MysteryBoxWinPercentage { get; set; } = 100f;
	public virtual string[] MysteryBoxRewards { get; set; } =
	[
	];
}
