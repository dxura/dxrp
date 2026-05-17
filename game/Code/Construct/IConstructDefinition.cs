using System.Text.Json;
using System.Threading.Tasks;
namespace Dxura.RP.Game;

public interface IConstructDefinition
{
	ConstructType Type { get; }

	Type ConstructComponentType { get; }
	Type DataType { get; }

	uint Limit { get; }
	string DisplayName { get; }
	string Description { get; }

	/// <summary>
	/// The current schema version this definition expects for its data.
	/// </summary>
	uint DataSchemaVersion => 1;

	/// <summary>
	/// Validate construct data 
	/// </summary>
	ConstructDataValidationResult Validate( IConstructData data );

	/// <summary>
	/// Create and configure a new construct instance
	/// </summary>
	IConstruct? CreateConstruct( long owner, IConstructData data, Vector3 position, Rotation rotation, bool isPreview = false );

	/// <summary>
	/// Optional migration hook to transform older schema payloads to the current data type.
	/// Return null to indicate failure.
	/// </summary>
	IConstructData? Migrate( uint fromVersion, JsonElement data, ConstructDataSerializer serializer )
	{
		return null;
	}
}
