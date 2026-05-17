using Dxura.RP.Game.UI;
namespace Dxura.RP.Game;

public partial class Player : IContextualObject, Component.IPressable
{
	public Vector3 ContextPosition => WorldPosition + Controller.BodyHeight * 0.60f * Vector3.Up;
	public bool ShouldShow()
	{
		return !IsDead && !IsLocalPlayer && !string.IsNullOrWhiteSpace( Job.Interaction );
	}
	public float ContextMaxDistance => Config.Current.Game.PlayerInteractDistance;
	public bool LookOpacity => false;
	public string InputHint => "use";
	public string? DisplayText => Job.Interaction;


	private void OnStartInteract()
	{
		Controller.ColliderObject.GetOrAddComponent<PressablePropagate>();
	}

	public bool Press( IPressable.Event e )
	{
		if ( string.IsNullOrWhiteSpace( Job.Interaction ) )
		{
			return false;
		}

		if ( string.Equals( Job.Interaction, "HitRequest", StringComparison.OrdinalIgnoreCase ) )
		{
			var hitUi = GameManager.ShowUi<HitRequestModal>();
			hitUi?.Hitman = this;
			return true;
		}

		return false;
	}
}
