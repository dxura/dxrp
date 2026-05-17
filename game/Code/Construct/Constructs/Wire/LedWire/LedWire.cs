namespace Dxura.RP.Game.Wire;

[Title( "LED" )]
[Category( "Wire" )]
[Icon( "lightbulb" )]
public class LedWire() : BaseWireConstruct( ConstructType.LedWire )
{

	public override Vector3 GetPortPosition()
	{
		return GameObject.WorldPosition + WorldRotation.Down * 1f;
	}
	private LedWireData _data = new();

	[Property]
	public bool LedState { get; set; }

	public override string Name => "LED";

	[Property]
	public ModelRenderer ModelRenderer { get; set; } = null!;

	[WireInput( "state" )]
	public bool State
	{
		set
		{
			if ( LedState == value )
			{
				return;
			}

			LedState = value;
			BroadcastState( LedState );
		}
		get => LedState;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastState( bool state )
	{
		LedState = state;
		UpdateColor();
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as LedWireData ?? new LedWireData();
		UpdateColor();
	}

	private void UpdateColor()
	{
		if ( !ModelRenderer.IsValid() )
		{
			return;
		}

		var targetColor = LedState ? _data.OnColor : _data.OffColor;
		ModelRenderer.Tint = targetColor;
	}
}
