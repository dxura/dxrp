namespace Dxura.RP.Game.Tools;

[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public class ToolInspectorAttribute( Type type ) : Attribute
{
	public Type Type { get; set; } = type;
}
