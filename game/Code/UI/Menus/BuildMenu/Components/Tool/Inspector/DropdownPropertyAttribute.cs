namespace Dxura.RP.Game.UI;

[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter )]
public class DropdownPropertyAttribute( params string[] options ) : Attribute
{
	public string[] Options { get; set; } = options;

}
