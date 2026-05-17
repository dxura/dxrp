namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	// Radio
	public virtual string[] RadioStations { get; set; } =
	[
		"Smooth Jazz|https://ais-edge89-dal02.cdnstream.com/2124_128.mp3",
		"Dance FM|https://broadcast.dancefmlive.com/radio/8010/radio.mp3",
		"Oldies 103 FM|https://das-edge14-live365-dal02.cdnstream.com/a88248",
		"Radio Paradise|https://stream-dc1.radioparadise.com/mp3-128",
		"90's HipHop|https://streams.90s90s.de/hiphop/mp3-192/streams.90s90s.de/",
		"EDM Hits|http://geostream.cdn.shoutdrive.com/sd-mp3",
		"Fox News|https://live.amperwave.net/direct/foxnewsradio-foxnewsradioaac-imc",
	];
}
