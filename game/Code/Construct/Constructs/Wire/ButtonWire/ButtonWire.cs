using Dxura.RP.Game.UI;
using System.Threading.Tasks;
namespace Dxura.RP.Game.Wire;

[Title( "Button" )]
[Category( "Wire" )]
[Icon( "cable" )]
public class ButtonWire() : BaseWireConstruct( ConstructType.ButtonWire ), Component.IPressable, IContextualObject, IWireEvents
{
	[Property] [RequireComponent]
	private HighlightOutline Highlight { get; set; } = null!;

	[Property] [Group( "Effects" )]
	public SoundEvent? PressSound { get; set; }

	private ButtonWireData _data = new();

	[WireOutput( "out" )]
	private float Out { get; set; }

	public override string Name => "Button";

	[Property]
	private float PressDepth { get; set; } = 2.0f;

	[Property]
	private float PressDuration { get; set; } = 0.08f;

	[Property]
	public GameObject ModelGameObject = null!;

	private bool _isAnimating;

	private bool _pressQueued;

	protected override void OnStart()
	{
		base.OnStart();
		Highlight.Enabled = false;
	}

	public bool Press( IPressable.Event e )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "button:use", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return false;
		}

		OnPressHost();

		return true;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastPressEffects()
	{
		if ( _isAnimating )
		{
			return;
		}

		_ = AnimatePressAsync();

		PressSound?.Play( WorldPosition );
	}

	private async Task AnimatePressAsync()
	{
		_isAnimating = true;
		var pressedPosition = ModelGameObject.LocalRotation.Up * -PressDepth;

		// Animate in
		await AnimatePosition( Vector3.Zero, pressedPosition, PressDuration );

		// Animate out
		await AnimatePosition( pressedPosition, Vector3.Zero, PressDuration );

		_isAnimating = false;
	}

	private async Task AnimatePosition( Vector3 from, Vector3 to, float duration )
	{
		if ( !GameObject.IsValid() )
		{
			return;
		}

		var elapsed = 0f;
		while ( elapsed < duration )
		{
			var t = elapsed / duration;
			ModelGameObject.LocalPosition = Vector3.Lerp( from, to, t );
			await GameTask.Delay( 500 );
			elapsed += Time.Delta;
		}
		ModelGameObject.LocalPosition = to;
	}

	[Rpc.Host]
	private void OnPressHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:button:use", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		// Server-side distance check to prevent remote button interaction
		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		var distance = player.WorldPosition.Distance( WorldPosition );
		if ( distance > Config.Current.Game.ReachDistance )
		{
			return;
		}

		_pressQueued = true;
	}

	public void OnPreWirePropagate()
	{
		if ( !_pressQueued )
		{
			return;
		}

		Out = Math.Abs( Out - _data.OffValue ) < GateWireDefinition.GateToleranceThreshold ? _data.OnValue : _data.OffValue;
		BroadcastPressEffects();
	}
	public void OnPostWirePropagate()
	{
		if ( _data.Toggle )
		{
			_pressQueued = false;
			return;
		}

		Out = _data.OffValue;
		_pressQueued = false;
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as ButtonWireData ?? new ButtonWireData();
	}

	public void Hover( IPressable.Event e )
	{
		Highlight.Enabled = true;
	}

	public void Blur( IPressable.Event e )
	{
		Highlight.Enabled = false;
	}

	public Vector3 ContextPosition => WorldPosition + Vector3.Up * 6f;
	public bool LookOpacity => false;
	public float ContextMaxDistance => 100f;

	public string? DisplayText => string.IsNullOrWhiteSpace( _data.Label ) ? null : _data.Label;

}
