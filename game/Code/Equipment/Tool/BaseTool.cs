using Dxura.RP.Game.Equipments;

namespace Dxura.RP.Game.Tools;

public abstract class BaseTool
{
	private static readonly Dictionary<string, Dictionary<string, object>> SavedSettings = new();

	public virtual string Attack1Control => "";
	public virtual string Attack2Control => "";
	public virtual string ReloadControl => "";
	public required ToolEquipment Tool;

	public static BaseTool? CurrentTool;

	public virtual void OnEquip()
	{
		CurrentTool = this;

		if ( SavedSettings.TryGetValue( GetType().Name, out var settings ) )
		{
			var properties = TypeLibrary.GetSerializedObject( this );
			foreach ( var prop in properties )
			{
				if ( !prop.HasAttribute<PropertyAttribute>() )
				{
					continue;
				}
				if ( settings.TryGetValue( prop.Name, out var value ) )
				{
					try
					{
						prop.SetValue( value );
					}
					catch
					{
						// ignored
					}
				}
			}
		}
	}

	public virtual void OnUnequip()
	{
		var settings = new Dictionary<string, object>();
		var properties = TypeLibrary.GetSerializedObject( this );
		foreach ( var prop in properties )
		{
			if ( !prop.HasAttribute<PropertyAttribute>() )
			{
				continue;
			}
			try
			{
				var value = prop.GetValue<object>();
				if ( value != null )
				{
					settings[prop.Name] = value;
				}
			}
			catch
			{
				// ignored
			}
		}
		if ( settings.Any() )
		{
			SavedSettings[GetType().Name] = settings;
		}
	}

	public void ResetSettings()
	{
		SavedSettings.Remove( GetType().Name );
		var newInstance = TypeLibrary.Create<BaseTool>( GetType() );
		var properties = TypeLibrary.GetSerializedObject( this );
		var newProperties = TypeLibrary.GetSerializedObject( newInstance );

		foreach ( var prop in properties )
		{
			if ( !prop.HasAttribute<PropertyAttribute>() )
			{
				continue;
			}
			var newProp = newProperties.FirstOrDefault( p => p.Name == prop.Name );
			if ( newProp != null )
			{
				try { prop.SetValue( newProp.GetValue<object>() ); }
				catch {}
			}
		}
	}

	public virtual void OnToolUpdate()
	{
	}

	public virtual void OnToolFixedUpdate()
	{
	}

	public virtual void PrimaryUseStart()
	{
	}

	public virtual void PrimaryUseUpdate()
	{
	}

	public virtual void PrimaryUseEnd()
	{
	}

	public virtual void SecondaryUseStart()
	{
	}

	public virtual void SecondaryUseUpdate()
	{
	}

	public virtual void SecondaryUseEnd()
	{
	}

	public virtual void ReloadUseStart()
	{
	}

	public virtual void ReloadUseUpdate()
	{
	}

	public virtual void ReloadUseEnd()
	{
	}

	public string GetName()
	{
		return GetText( TypeLibrary.GetAttribute<ToolAttribute>( GetType() ).Title );
	}

	public string GetDescription()
	{
		return GetText( TypeLibrary.GetAttribute<ToolAttribute>( GetType() ).Description );
	}

	public string GetLongDescription()
	{
		var attr = TypeLibrary.GetAttribute<DescriptionAttribute>( GetType() );
		if ( string.IsNullOrWhiteSpace( attr?.Value ) )
		{
			return GetDescription();
		}

		return GetText( attr.Value );
	}

	public string GetGroup()
	{
		return TypeLibrary.GetAttribute<ToolAttribute>( GetType() ).Group;
	}

	public List<(string, string)> GetControls()
	{
		var controls = new List<(string, string)>();

		if ( !string.IsNullOrEmpty( Attack1Control ) )
		{
			controls.Add( ("Attack1", GetText( Attack1Control )) );
		}

		if ( !string.IsNullOrEmpty( Attack2Control ) )
		{
			controls.Add( ("Attack2", GetText( Attack2Control )) );
		}

		if ( !string.IsNullOrEmpty( ReloadControl ) )
		{
			controls.Add( ("Reload", GetText( ReloadControl )) );
		}

		return controls;
	}

	private static string GetText( string? text )
	{
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			return string.Empty;
		}

		return text.StartsWith( '#' ) ? Language.GetPhrase( text[1..] ) : text;
	}

	public string GetControlsHash()
	{
		return $"{Attack1Control}{Attack2Control}{ReloadControl}";
	}

	/// <summary>
	/// Performs a fresh raytrace from the player's eye position.
	/// This ensures tools operate on exactly what the player is aiming at when they click.
	/// </summary>
	protected SceneTraceResult PerformEyeTrace()
	{
		return Sandbox.Game.ActiveScene.Trace.Ray( Player.Local.AimRay, Config.Current.Game.ReachDistance )
			.IgnoreGameObjectHierarchy( Player.Local.GameObject )
			.WithoutTags( "invisible", "trigger" )
			.UseHitboxes()
			.Run();
	}

	/// <summary>
	/// Broadcasts Grabbed tag to the target GameObject. 
	/// </summary>
	protected void BroadcastGrabbed( GameObject target, bool enabled )
	{
		Tool.BroadcastGrabbedHost( target, enabled );
	}
}
