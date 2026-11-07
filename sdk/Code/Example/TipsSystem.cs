namespace Dxura.RP.Game.Addons.Example;

public sealed class TipsConfig
{
	public float IntervalSeconds { get; set; } = 300f;
	public List<string> Tips { get; set; } = [];
}

/// <summary>
/// System content: attached only while a game mode enables the "Tips" entry. Its config is the entry's BaseConfig
/// merged with the game mode's override, so operators change the tips without touching code.
/// </summary>
public sealed class TipsSystem : BaseSystem<TipsConfig>
{
	private TimeSince _lastTip;

	protected override void OnConfigChanged()
	{
		Log.Info( $"[Example] Tips: {Config.Tips.Count} tips every {Config.IntervalSeconds}s" );
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost || Config.Tips.Count == 0 || _lastTip < Config.IntervalSeconds )
		{
			return;
		}

		_lastTip = 0;
		var tip = Random.Shared.FromList( Config.Tips );
		foreach ( var player in GameUtils.Players )
		{
			player.SendMessage( $"Tip: {tip}" );
		}
	}
}
