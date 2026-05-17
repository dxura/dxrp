namespace Dxura.RP.Game;

public static class Constants
{

	//
	// Statuses (Constants)
	//

	public const string AfkStatus = "afk";
	public const string GodStatus = "god";
	public const string CloakStatus = "cloak";
	public const string WantedStatus = "wanted";
	public const string PrisonerStatus = "prisoner";
	public const string BandageStatus = "bandage";
	public const string GunLicenseStatus = "gun_license";
	public const string WarrantStatus = "warrant";
	public const string RaidBlockStatus = "raid_block";
	public const string GaggedStatus = "gagged";
	public const string HitAcceptedStatus = "hit_accepted";
	public const string WeedHighStatus = "weed_high";
	public const string SatiatedStatus = "satiated";
	public const string IncognitoStatus = "incognito";
	public const string SurrenderStatus = "surrender";
	public const string DrunkStatus = "drunk";
	public const string FreezeStatus = "freeze";
	
	// API
	public static string ApiBaseUrl => ServerApiLink.Endpoint switch
	{
		ApiEndpoint.Local => "http://localhost:8080",
		ApiEndpoint.Staging => "https://staging-api.dxrp.net",
		_ => "https://api.dxrp.net"
	};
	public const int ApiServerSyncInterval = 10;
	public const string ApiSboxSteamIdHeader = "X-Sbox-SteamId";
	public const string ApiSboxAuthTokenHeader = "X-Sbox-Auth-Token";
	public const string ApiServerTokenHeader  = "X-Server-Token";
	public const string ApiTenantIdHeader = "X-Tenant";
	
	public const string OfficialTenantId = "11111111-1111-1111-1111-111111111111";
	
	// Misc
	public static string BaseWebsiteUrl => ServerApiLink.Endpoint switch
	{
		ApiEndpoint.Production => "https://dxrp.net",
		_ => "https://staging.dxrp.net"
	};

	//
	// Tags (Constants)
	//

	public const string PlayerTag = "player";
	public const string MapTag = "map";
	public const string RagdollTag = "ragdoll";

	public const string ConstructTag = "construct";

	public const string EntityTag = "entity";
	public const string RestrictedEntity = "restricted_entity"; // Modifier tag for entities which allow permitted (and can be destroyed)

	public const string HandsInteractTag = "hands_interact";
	public const string BuildInteractTag = "build_interact";

	public const string GarbageTag = "garbage";
	public const string NonRecyclableTag = "non_recyclable";

	public const string GrabbedTag = "grabbed";
	public const string PryingTag = "prying";

	public const string PocketTag = "pocket";
	public const string PocketItemTag = "pocket_item";

	public const string FadedTag = "faded";

	public const string NoCollideTag = "no_collide";

	public const string OccludeTag = "occlude";
	public const string OccludableTag = "occludable";
	public const string CostlyTag = "costly";

	public const string PlayerClip = "playerclip";
	public const string InvisibleTag = "invisible";

	public static readonly string[] TraceIgnoreTags = ["trigger", "movement", "playercolliders", FadedTag, InvisibleTag];
}
