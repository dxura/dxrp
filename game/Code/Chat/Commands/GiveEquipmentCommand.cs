using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class GiveEquipmentCommand : ICommand
{
	public string Command => "giveequip";
	public string[] Aliases => ["giveweapon"];
	public string Help => "/giveequip <equipment> [player] — ex: taser, handcuffs, hand_cuffs (debug)";
	public bool IsUsableWhileRestricted => true;
	public Permission[] RequiredPermissions => [Permission.DebugFull];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( args.Length < 1 )
		{
			caller.SendMessage( Help );
			return true;
		}

		var equipmentInput = NormalizeEquipmentInput( args[0] );
		var target = caller;

		if ( args.Length >= 2 )
		{
			target = CommandHelper.ResolvePlayer( caller, string.Join( ' ', args[1..] ) );
			if ( !target.IsValid() )
			{
				return true;
			}
		}

		var resource = GameModeEquipments.FindByIdentifier( equipmentInput );
		Equipment? equipment;

		if ( resource != null && !string.IsNullOrWhiteSpace( resource.PrefabPath() ) )
		{
			equipment = target.GiveHost( resource );
		}
		else
		{
			var prefabPath = ResolvePrefabPath( equipmentInput );
			if ( string.IsNullOrWhiteSpace( prefabPath ) )
			{
				caller.Error( $"Equipment not found: {equipmentInput}" );
				return true;
			}

			var identifier = resource?.Identifier() ?? equipmentInput;
			equipment = target.GiveFromPrefabHost( identifier, prefabPath );
		}

		if ( !equipment.IsValid() )
		{
			caller.Error( $"Failed to give equipment: {equipmentInput}" );
			return true;
		}

		caller.Success( $"Gave {equipmentInput} to {target.DisplayName}" );
		return true;
	}

	private static string NormalizeEquipmentInput( string input )
	{
		return input.Trim().ToLowerInvariant() switch
		{
			"handcuffs" or "handcuff" or "cuffs" or "menottes" or "menotte" => "hand_cuffs",
			_ => input.Trim().ToLowerInvariant()
		};
	}

	private static string? ResolvePrefabPath( string input )
	{
		if ( input.Contains( '/' ) )
		{
			return GameObject.GetPrefab( input ).IsValid() ? input : null;
		}

		var candidates = new[]
		{
			$"gameplay/equipment/job/{input}/w_{input}.prefab",
			$"gameplay/equipment/weapons/{input}/w_{input}.prefab",
			$"gameplay/equipment/default/{input}/w_{input}.prefab"
		};

		return candidates.FirstOrDefault( path => GameObject.GetPrefab( path ).IsValid() );
	}
}
