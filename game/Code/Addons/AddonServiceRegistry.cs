namespace Dxura.RP.Game.Addons;

public sealed class AddonServiceRegistry : GameObjectSystem<AddonServiceRegistry>
{
	private readonly List<Type> _componentTypes = new();
	
	public AddonServiceRegistry( Scene scene ) : base( scene )
	{
		Listen( Stage.SceneLoaded, 100, RegisterServices, "Register Addon Services" );
	}

	private void RegisterServices()
	{
		_componentTypes.Clear();

		var componentTypes = TypeLibrary.GetTypes<Component>();

		foreach ( var type in componentTypes.Where( t => !t.IsAbstract && t.TargetType != null ) )
		{
			if ( TypeLibrary.GetAttribute<AddonServiceAttribute>( type.TargetType ) == null )
			{
				continue;
			}

			RegisterComponent( type.TargetType );
		}

		AttachServicesToGameManagerRoot();

		Log.Info( $"Registered {_componentTypes.Count} addon services" );
	}

	public void RegisterComponent( Type componentType )
	{
		if ( !typeof( Component ).IsAssignableFrom( componentType ) || _componentTypes.Contains( componentType ) )
		{
			return;
		}

		_componentTypes.Add( componentType );
	}

	private void AttachServicesToGameManagerRoot()
	{
		if ( !GameManager.Instance.IsValid() )
		{
			return;
		}

		var coreObject = GameManager.Instance.GameObject.Root;
		if ( !coreObject.IsValid() )
		{
			return;
		}

		foreach ( var componentType in _componentTypes )
		{
			if ( coreObject.Components.Get( componentType ) != null )
			{
				continue;
			}

			coreObject.Components.Create( TypeLibrary.GetType( componentType ) );
		}
	}
}

[AttributeUsage( AttributeTargets.Class, Inherited = false )]
public sealed class AddonServiceAttribute : Attribute;
