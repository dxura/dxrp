namespace Dxura.RP.Game.Wire;

[Title( "Camera" )]
[Category( "Wire" )]
[Icon( "cable" )]
public class CameraWire() : BaseWireConstruct( ConstructType.CameraWire ), IWireEvents
{
	private CameraWireData _data = new();
	public override string Name => "Camera";

	[Property]
	public required CameraComponent Camera { get; set; }

	[WireOutput( "camera" )]
	public string Identifier { get; private set; } = "";

	protected override void OnStart()
	{
		base.OnStart();

		Identifier = $"{CameraWireDefinition.CameraPrefix}{_data.Identifier}";
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as CameraWireData ?? new CameraWireData();
		Identifier = $"{CameraWireDefinition.CameraPrefix}{_data.Identifier}";
	}
}
