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
	public const string StunStatus = "stun";
	
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
	public const string ClaimableEntityTag = "claimable_entity"; // Modifier tag for admin-spawned entities awaiting their first owner

	public const string HandsInteractTag = "hands_interact";
	public const string BuildInteractTag = "build_interact";

	public const string GarbageTag = "garbage";
	public const string NonRecyclableTag = "non_recyclable";

	public const string GrabbedTag = "grabbed";
	public const string LadderTag = "ladder";
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

	//
	// Equipment content IDs — match GameModeAddonContentDto.Id for the corresponding item
	//

	public static readonly Guid HandsEquipmentId    = new( "2e3ff5a0-f2a2-4036-9542-41b1f56d0b13" );
	public static readonly Guid MedkitEquipmentId   = new( "848f8ecd-5c8b-4b4c-9191-900ec948fe14" );
	public static readonly Guid CameraEquipmentId   = new( "83dd6a00-ea58-48db-a869-782d148fa81c" );
	public static readonly Guid ToolEquipmentId     = new( "37580dd6-fcbd-477a-99c2-5109c1da20d8" );
	public static readonly Guid KnifeEquipmentId    = new( "f06e0079-c9c7-4845-8580-a3b7a3b548a0" );
	public static readonly Guid UspEquipmentId      = new( "823befaf-d245-466c-b981-772d3bd1c47b" );
	public static readonly Guid Mp5EquipmentId      = new( "436a800c-1a82-4778-9f2f-e0db69336623" );
	public static readonly Guid SpaghelliEquipmentId = new( "6e3e22d7-9892-4e3c-a03e-074f22b10103" );
	public static readonly Guid M4a1EquipmentId     = new( "b2842f29-1db5-43d8-8ac7-886757f59380" );

	//
	// Entity content IDs — match GameModeAddonContentDto.Id for the corresponding entity
	//

	public static readonly Guid MysteryBoxEntityId = new( "927bc1e4-1bf1-4972-b1f1-6262fefa5075" );
}
