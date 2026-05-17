namespace Dxura.RP.Game;

public partial class Npc
{
	/// <summary>
	/// Time in seconds before the NPC's body is removed after death
	/// </summary>
	[Property]
	public float DestroyAfterDeathTime { get; set; } = 5.0f;

	/// <summary>
	/// NPC display name
	/// </summary>
	[Property]
	public virtual string DisplayName { get; set; } = "NPC";

	/// <summary>
	/// NPC color for UI/display purposes
	/// </summary>
	[Property]
	public virtual Color Color { get; set; } = Color.Gray;
}
