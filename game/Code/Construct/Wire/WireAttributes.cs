namespace Dxura.RP.Game.Wire;

[AttributeUsage( AttributeTargets.Property )]
[CodeGenerator( CodeGeneratorFlags.WrapPropertySet | CodeGeneratorFlags.Instance, "OnWireOutputSet" )]
public class WireOutputAttribute( string id ) : Attribute
{
	public string Id { get; } = id;
}

[AttributeUsage( AttributeTargets.Property )]
public class WireInputAttribute( string id ) : Attribute
{
	public string Id { get; } = id;
}
