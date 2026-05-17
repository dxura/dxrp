using Dxura.RP.Shared;
namespace Dxura.RP.Game.Tools;

public class ToolAttribute( string title, string description, string group = "" ) : Attribute
{
	public string Title { get; set; } = title;
	public string Description { get; set; } = description;
	public string Group { get; set; } = group;

	public ToolCategory Category { get; set; } = ToolCategory.Default;

	// Restrictions
	public Permission[] RequiredPermissions { get; set; } = [];
	public int MinimumLevel { get; set; }
}
