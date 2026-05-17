using Dxura.RP.Game.Wire;
namespace Dxura.RP.Game;

[Title( "Light" )]
[Category( "Lighting" )]
[Icon( "lightbulb" )]
public class Light() : BaseConstruct( ConstructType.Light ), IWireComponent
{
	private LightData _data = new();

	[Property]
	public Sandbox.Light? LightComponent { get; set; }

	[Property]
	public ModelRenderer ModelRenderer { get; set; } = null!;

	[Property]
	public GameObject LightRoot { get; set; } = null!;

	private float Red { get; set; }
	private float Green { get; set; }
	private float Blue { get; set; }

	[Property]
	public bool LightEnabled { get; set; } = true;

	public override void OnOcclusionChanged( bool occlude )
	{
		base.OnOcclusionChanged( occlude );

		UpdateLightEnabledState( occlude );
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as LightData ?? new LightData();

		ModelRenderer.Tint = _data.Color;

		UpdateLightComponent();
	}

	protected override void OnStart()
	{
		base.OnStart();

		if ( !IsPreview && Networking.IsHost )
		{
			Wire.Wire.Current.RegisterComponent( this );
		}
	}

	protected override void OnDestroy()
	{
		if ( !IsPreview && Networking.IsHost )
		{
			Wire.Wire.Current?.UnregisterComponent( this );
		}

		base.OnDestroy();
	}

	// Wire implementation
	public string Name => "Light";
	public IEnumerable<WirePort> GetInputPorts()
	{
		yield return WirePort.Input( "red", WireType.Number );
		yield return WirePort.Input( "green", WireType.Number );
		yield return WirePort.Input( "blue", WireType.Number );
		yield return WirePort.Input( "enabled", WireType.Bool );
	}

	public IEnumerable<WirePort> GetOutputPorts()
	{
		yield break;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastLightState( bool enabled )
	{
		LightEnabled = enabled;
		UpdateLightEnabledState();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastLightColor( Color color )
	{
		_data.Color = color;
		ModelRenderer.Tint = color;

		if ( LightComponent.IsValid() )
		{
			LightComponent.LightColor = color;
		}
	}

	public void OnWireInput( string inputId, WireValue value )
	{
		if ( inputId == "enabled" )
		{
			var newEnabled = value.Value as bool? ?? false;
			if ( LightEnabled == newEnabled )
			{
				return;
			}
			BroadcastLightState( newEnabled );
		}

		// Color management + ensure the light has been updated (TO CHECK ensure the light has been "BroadCast")
		// Using "/255f" just to be sure it's "user friendly" and the player get can value ~[0-255] from a color picker
		var newColor = new Color( Red, Green, Blue );
		var tintValue = 255f;

		// Ensure to have to good type and ensure to transform it as "float" to avoid any problem
		if ( value.Type.Name == "number" )
		{
			tintValue = (float)value.Value;
		}

		if ( inputId == "red" && tintValue >= 0 && tintValue <= 255 )
		{
			Red = Clamp( tintValue / 255f, 0f, 1f );
			newColor.r = Red;
		}

		if ( inputId == "green" && tintValue >= 0 && tintValue <= 255 )
		{
			Green = Clamp( tintValue / 255f, 0f, 1f );
			newColor.g = Green;
		}

		if ( inputId == "blue" && tintValue >= 0 && tintValue <= 255 )
		{
			Blue = Clamp( tintValue / 255f, 0f, 1f );
			newColor.b = Blue;
		}

		if ( inputId == "red" || inputId == "green" || inputId == "blue" )
		{
			BroadcastLightColor( newColor );
		}
	}

	public Vector3 GetPortPosition()
	{
		return WorldPosition;
	}

	public static float Clamp( float value, float min, float max )
	{
		if ( value < min )
		{
			return min;
		}
		if ( value > max )
		{
			return max;
		}
		return value;
	}

	// Method to update/refresh the light color component/render
	private void UpdateLightComponent()
	{
		LightComponent?.Destroy();
		LightComponent = _data.Type switch
		{
			LightType.Point => LightRoot.Components.Create<PointLight>(),
			LightType.Spot => LightRoot.Components.Create<SpotLight>(),
			_ => null!
		};
		LightComponent.LightColor = _data.Color;
		UpdateLightEnabledState();

		LightComponent.Shadows = false;

		switch ( _data.Type )
		{
			case LightType.Point:
				var pointLight = LightComponent as PointLight;
				if ( !pointLight.IsValid() )
				{
					return;
				}

				pointLight.Radius = _data.Radius;
				pointLight.Attenuation = _data.Attenuation;
				break;
			case LightType.Spot:
				var spotLight = LightComponent as SpotLight;
				if ( !spotLight.IsValid() )
				{
					return;
				}

				spotLight.Radius = _data.Radius;
				spotLight.Attenuation = _data.Attenuation;
				spotLight.ConeOuter = _data.Cone;
				break;
		}
	}

	private void UpdateLightEnabledState( bool? occluded = null )
	{
		if ( LightComponent.IsValid() )
		{
			LightComponent.Enabled = LightEnabled && !(occluded ?? GameObject.Tags.Has( Constants.OccludeTag ));
		}
	}
}
