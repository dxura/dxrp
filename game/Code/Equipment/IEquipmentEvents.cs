namespace Dxura.RP.Game;

public interface IEquipmentEvents : ISceneEvent<IEquipmentEvents>
{
	void OnEquipmentDropped( DroppedEquipment dropped, Player? player ) {}

	void OnEquipmentPickedUp( Player player, DroppedEquipment dropped, Equipment equipment ) {}


	void OnEquipmentDeployed( Equipment equipment ) {}

	void OnEquipmentHolstered( Equipment equipment ) {}

	void OnEquipmentDestroyed( Equipment equipment ) {}
}
