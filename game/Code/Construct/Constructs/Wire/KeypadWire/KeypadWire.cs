namespace Dxura.RP.Game.Wire;

[Title( "Keypad" )]
[Category( "Wire" )]
[Icon( "cable" )]
public class KeypadWire() : BaseWireConstruct( ConstructType.KeypadWire ), IWireEvents
{
	[Property]
	private SoundEvent? FailSound { get; set; }
	[Property]
	private SoundEvent? SuccessSound { get; set; }
	[Property]
	private SoundEvent? SetupSound { get; set; }

	[WireOutput( "out" )]
	private float Out { get; set; }

	[WireOutput( "input" )]
	private string Input { get; set; } = "";

	public override string Name => "Keypad";

	[Property]
	public bool Initialized { get; set; }

	public TimeSince TimeSinceLastFail { get; set; } = 100f;
	public TimeSince TimeSinceLastSuccess { get; set; } = 100f;

	private string _code = "";

	private KeypadWireData _data = new();

	public void OnPostWirePropagate()
	{
		if ( !_data.Toggle && Out == _data.OnValue )
		{
			Out = _data.OffValue;
		}
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as KeypadWireData ?? new KeypadWireData();
	}

	[Rpc.Host]
	public void ResetCodeHost( string code )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:keypad", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var isCorrectCode = code.Equals( _code );

		if ( !isCorrectCode )
		{
			return;
		}

		// Only allow owners or friends with construct permissions to reset the code
		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() || FriendSystem.Instance.IsValid() && !FriendSystem.Instance.HasConstructPermission( Owner, player.SteamId ) )
		{
			return;
		}

		// Server-side distance check to prevent remote keypad interaction
		var distance = player.WorldPosition.Distance( WorldPosition );
		if ( distance > Config.Current.Game.ReachDistance )
		{
			return;
		}

		Initialized = false;
		_code = "";
		Out = _data.OffValue;
		SetupSound.BroadcastHost( WorldPosition, GameObject );
		BroadcastInitializedState( false );
	}

	[Rpc.Host]
	public void SubmitCodeHost( string code )
	{
		var caller = Rpc.Caller;
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:keypad", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		// Server-side distance check to prevent remote keypad interaction
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

		if ( !Initialized )
		{
			_code = code;
			Initialized = true;
			SetupSound.BroadcastHost( WorldPosition, GameObject );
			BroadcastInitializedState( true );
			return;
		}

		Input = code;
		var isCorrectCode = code.Equals( _code );

		using ( Rpc.FilterInclude( c => c == caller ) )
		{
			OnSubmitCodeResultCaller( isCorrectCode );
		}

		if ( !isCorrectCode )
		{
			Out = _data.OffValue;
			FailSound.BroadcastHost( WorldPosition, GameObject );
			return;
		}

		Out = Out == _data.OnValue ? _data.OffValue : _data.OnValue;
		SuccessSound.BroadcastHost( WorldPosition, GameObject );

		using ( Rpc.FilterInclude( c => c == caller ) )
		{
			SendSuccessfulCodeToCaller( code );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void OnSubmitCodeResultCaller( bool didSucceed )
	{
		if ( didSucceed )
		{
			TimeSinceLastSuccess = 0f;
		}
		else
		{
			TimeSinceLastFail = 0f;
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void SendSuccessfulCodeToCaller( string code )
	{
		OnSuccessfulCodeReceived?.Invoke( code );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastInitializedState( bool initialized )
	{
		Initialized = initialized;
	}

	public event Action<string>? OnSuccessfulCodeReceived;

}
