namespace Dxura.RP.Game;

using Sandbox;

/// <summary>
/// Class for all non-player characters in the game
/// </summary>
public partial class Npc : Component, IDamageEvents, IAreaDamageReceiver, IDescription
{
	/// <summary>
	/// Called on component start
	/// </summary>
	protected override void OnStart()
	{
		OnStartAnimation();
		OnStartEffects();
		OnStartBody();
	}

	protected override void OnUpdate()
	{
		if ( !Health.IsValid() || Health.State == LifeState.Dead )
		{
			return;
		}

		OnUpdateEffects();
		OnUpdateAnimation();

		if ( !Networking.IsHost )
		{
			return;
		}

		OnUpdateAi();
	}
}
