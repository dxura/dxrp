using Dxura.RP.Game.UI;
namespace Dxura.RP.Game;

public class CountdownContext : Component, IContextualObject
{

	[Property]
	public TimeUntil CountdownTime { get; set; }

	[Property]
	public string? DisplayPrefix { get; set; }

	public string DisplayText
	{
		get
		{
			var prefix = string.IsNullOrWhiteSpace( DisplayPrefix ) ? string.Empty : DisplayPrefix.StartsWith( '#' ) ? Language.GetPhrase( DisplayPrefix[1..] ) : DisplayPrefix;
			return string.Format( Language.GetPhrase( "context.countdown.seconds" ), prefix, (int)CountdownTime.Relative ).Trim();
		}
	}

	public bool LookOpacity => false;
	public Vector3 ContextPosition => WorldPosition;
	public float ContextMaxDistance => 200.0f;
}
