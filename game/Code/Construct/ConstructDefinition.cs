using Dxura.RP.Shared;
namespace Dxura.RP.Game;

/// <summary>
/// Generic construct definition with type safety
/// </summary>
public abstract class ConstructDefinition<TConstruct, TData> : IConstructDefinition
	where TConstruct : BaseConstruct
	where TData : IConstructData, new()
{
	public abstract ConstructType Type { get; }
	public Type ConstructComponentType => typeof( TConstruct );
	public Type DataType => typeof( TData );

	public abstract uint Limit { get; }
	public virtual int? RankRestrictionOrder { get; } = null;

	public virtual string DisplayName => Type.ToString();
	public virtual string Description => $"A {Type} construct";
	public virtual uint DataSchemaVersion => 1;

	public virtual ConstructDataValidationResult Validate( IConstructData data )
	{
		return data is not TData typedData ? ConstructDataValidationResult.Failure( "Invalid data type" ) : ValidateTyped( typedData );
	}

	public IConstruct? CreateConstruct( long owner, IConstructData data, Vector3 position, Rotation rotation, bool isPreview = false )
	{
		if ( data is not TData typedData )
		{
			return null;
		}

		if ( RankRestrictionOrder.HasValue && !RankSystem.IsRankOrAbove( owner, RankRestrictionOrder.Value ) )
		{
			return null;
		}

		if ( !isPreview && !CanOwnerPlace( owner ) )
		{
			return null;
		}

		var gameObject = CreateConstructInternal( typedData, position, rotation );
		var construct = gameObject?.GetComponent<IConstruct>();

		if ( construct != null && construct.IsValid() )
		{
			construct.Initialize( owner, isPreview );
		}

		return construct;
	}

	protected virtual bool CanOwnerPlace( long owner ) => true;
	protected abstract ConstructDataValidationResult ValidateTyped( TData data );
	protected abstract GameObject? CreateConstructInternal( TData data, Vector3 position, Rotation rotation );
}
