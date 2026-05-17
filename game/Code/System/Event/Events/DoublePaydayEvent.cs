namespace Dxura.RP.Game.System.Events;

/// <summary>
/// Event that doubles all job salaries for a limited time
/// </summary>
public class DoublePaydayEvent : BaseEvent
{
	public override string Identifier => "double_payday";
	public override string Name => "Double Payday";
	public override string Description => "All job salaries are doubled for a limited time!";
	public override float Duration => 300f; // 5 minutes
	public override int Weight => 80;

	private float _originalMultiplier;

	protected override void OnStart()
	{
		// Store the original multiplier
		_originalMultiplier = GameManager.Instance.SalaryMultiplier;

		// Double all salaries
		GameManager.Instance.SalaryMultiplier *= 2.0f;

		Log.Info( $"Double Payday event started. Salary multiplier changed from {_originalMultiplier} to {GameManager.Instance.SalaryMultiplier}" );
	}

	protected override void OnEnd()
	{
		// Restore the original multiplier
		GameManager.Instance.SalaryMultiplier = _originalMultiplier;

		Log.Info( $"Double Payday event ended. Salary multiplier restored to {_originalMultiplier}" );
	}
}
