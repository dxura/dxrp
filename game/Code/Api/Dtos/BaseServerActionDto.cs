using System.Text.Json.Serialization;
namespace Dxura.RP.Shared;

[JsonPolymorphic(
	TypeDiscriminatorPropertyName = "$type",
	UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType
)]
[JsonDerivedType( typeof( RestartServerActionDto ), "restart" )]
[JsonDerivedType( typeof( BroadcastMessageActionDto ), "broadcast" )]
[JsonDerivedType( typeof( SanctionActionDto ), "sanction" )]
[JsonDerivedType( typeof( UpdateServerInfoActionDto ), "updates_server_info" )]
[JsonDerivedType( typeof( UpdateRanksActionDto ), "rank_snapshot" )]
[JsonDerivedType( typeof( UpdateGameModeActionDto ), "update_game_mode" )]
[JsonDerivedType( typeof( RankAssignmentActionDto ), "rank_assignment" )]
[JsonDerivedType( typeof( SetBalanceActionDto ), "set_balance" )]
[JsonDerivedType( typeof( SetLevelActionDto ), "set_level" )]
[JsonDerivedType( typeof( BackupRestoredActionDto ), "backup_restored" )]
public class BaseServerActionDto;
