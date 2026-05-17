namespace Dxura.RP.Game.Wire;

[Title( "Notifier" )]
[Category( "Wire" )]
[Icon( "notifications" )]
public class NotifierWire() : BaseWireConstruct( ConstructType.NotifierWire )
{
	private NotifierWireData _data = new();

	[WireInput( "notify" )]
	public object Notify
	{
		set
		{
			// Ignore falsy values to prevent unnecessary notifications
			if ( _data.IgnoreFalsyValue && value is false or 0 or 0f )
			{
				return; // Ignore false values
			}

			var owner = GameUtils.GetPlayerById( Owner );
			if ( !owner.IsValid() )
			{
				return;
			}

			var message = _data.Message;

			if ( _data.IncludeValue )
			{
				message = $"{message} ({value})";
			}

			owner.Warn( message );
		}
		get => false; // This is just a trigger, no need to store state
	}

	public override string Name => $"Notifier ({_data.Message})";

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as NotifierWireData ?? new NotifierWireData();
	}
}
