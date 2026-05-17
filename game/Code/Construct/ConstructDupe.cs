namespace Dxura.RP.Game;

public record ConstructDupe
{
	public string Name { get; set; } = string.Empty;
	public string Game { get; set; } = string.Empty;
	public int Version { get; set; } = 2;
	public string? Author { get; set; }
	public string? Map { get; set; }

	public Vector3 ReferencePoint { get; set; }

	public IEnumerable<ConstructDupeItem> Items { get; set; } = new List<ConstructDupeItem>();
	public IEnumerable<ConstructDupeWireConnection> WireConnections { get; set; } = new List<ConstructDupeWireConnection>();

	public float GetCooldown()
	{
		return Items.Count() > 5 ? Config.Current.Game.DupeCooldown : Config.Current.Game.ConstructCooldown;
	}
}

public record ConstructDupeItem
{
	public Guid Id { get; set; }
	public ConstructType Type { get; set; }
	public long Owner { get; set; }
	public Vector3 Position { get; set; }
	public Rotation Rotation { get; set; }

	public string DataJson { get; set; } = string.Empty;
}

public record ConstructDupeWireConnection
{
	public Guid SourceId { get; set; }
	public string? OutputName { get; set; }
	public string OutputId { get; set; } = string.Empty;
	public Guid TargetId { get; set; }
	public string? InputName { get; set; }
	public string InputId { get; set; } = string.Empty;
	public Color Color { get; set; } = Color.Red;
	public float Thickness { get; set; } = Wire.Wire.MaxWireThickness;
	public float Opacity { get; set; } = Wire.Wire.MaxWireOpacity;
	public Vector3[]? Anchors { get; set; }
}
