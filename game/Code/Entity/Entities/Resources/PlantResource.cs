[AssetType( Name = "Plant", Extension = "plant", Category = "DXRP" )]
public class PlantResource : GameResource
{
	public static HashSet<PlantResource> All { get; set; } = new();

	[Property]
	public required Resource Resource { get; set; } = null!;

	[Property]
	public List<int> Stages { get; set; } = new();

	[Property]
	public int TimeToGrow { get; set; }

	[Property]
	public GameObject? Harvest { get; set; } = null!;

	protected override void PostLoad()
	{
		All.Add( this );
	}
}
