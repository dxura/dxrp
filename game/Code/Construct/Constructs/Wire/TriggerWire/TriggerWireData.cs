namespace Dxura.RP.Game.Wire;

public enum TriggerFilterType
{
	Everything,
	PlayerOnly,
	EntityOnly,
	ConstructOnly
}

public enum TriggerInfoSource
{
	Default,
	PlayerJob,
	PlayerWallet,
	Health,
	PlayerPocket,
	PlayerEquipment,
}

public record TriggerWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public float Range { get; set; } = TriggerWireDefinition.DefaultTriggerLaserWireRange;
	public TriggerFilterType FilterType { get; set; } = TriggerFilterType.Everything;
	public TriggerInfoSource InfoSource { get; set; } = TriggerInfoSource.Default;
}
