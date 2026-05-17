namespace Dxura.RP.Game.System.Events;

/// <summary>
/// Event that applies a discount to all purchasable items
/// </summary>
public class FlashSaleEvent : BaseEvent
{
	public override string Identifier => "flash_sale";
	public override string Name => "Flash Sale";
	public override string Description => "All purchasable items are 25% off for a limited time!";
	public override float Duration => 300f; // 5 minutes
	public override int Weight => 80;

	private float _originalPriceMultiplier;

	protected override void OnStart()
	{
		// Store original price multiplier
		_originalPriceMultiplier = GameManager.Instance.EntityPriceMultiplier;

		// Apply 25% discount
		GameManager.Instance.EntityPriceMultiplier *= 0.75f;

		Log.Info( $"Flash Sale event started. Price multiplier changed from {_originalPriceMultiplier} to {GameManager.Instance.EntityPriceMultiplier}" );
	}

	protected override void OnEnd()
	{
		// Restore original price multiplier
		GameManager.Instance.EntityPriceMultiplier = _originalPriceMultiplier;

		Log.Info( $"Flash Sale event ended. Price multiplier restored to {_originalPriceMultiplier}" );
	}
}
