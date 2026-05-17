namespace Dxura.RP.Game;

public class ScreenShaker : Component
{
	/// <summary>
	///     Get any <see cref="ScreenShaker" /> component on the main camera.
	/// </summary>
	public static ScreenShaker Main =>
		Sandbox.Game.ActiveScene?.Camera?.Components.Get<ScreenShaker>( FindMode.EnabledInSelf )!;

	private readonly List<ScreenShake> _list = new();

	/// <summary>
	///     Apply any screen shake effects to the specified camera.
	/// </summary>
	/// <param name="camera"></param>
	public void Apply( CameraComponent camera )
	{
		for ( var i = _list.Count; i > 0; i-- )
		{
			var entry = _list[i - 1];
			var keep = entry.Update( camera );
			if ( keep )
			{
				continue;
			}

			_list.RemoveAt( i - 1 );
		}
	}

	/// <summary>
	///     Add a new screen shake effect to the list.
	/// </summary>
	public void Add( ScreenShake shake )
	{
		_list.Add( shake );
	}
}
