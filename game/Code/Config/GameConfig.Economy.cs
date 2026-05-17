namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	//
	// Economy
	//
	public virtual float DrugDropSellTime { get; set; } = 10f;
	public virtual uint DrugDropMaxPrice { get; set; } = 350;
	public virtual uint DrugDropMinPrice { get; set; } = 175;
	public virtual uint GarbageRubbishPaymentPrice { get; set; } = 20;
	public virtual int DrugDropPriceChangeCycle { get; set; } = 1800; // 30 minutes
	public virtual bool DropMoneyUsesBankForExcess { get; set; } = false;
}

