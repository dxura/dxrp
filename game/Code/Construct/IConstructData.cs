namespace Dxura.RP.Game;

public interface IConstructData
{
	/// <summary>
	/// Schema version for this data type to support forward-compatible migration and validation.
	/// Bump when making breaking changes to the data contract.
	/// </summary>
	public uint SchemaVersion { get; }
}

public struct EmptyConstructData : IConstructData
{
	public readonly uint SchemaVersion => 1;
}
