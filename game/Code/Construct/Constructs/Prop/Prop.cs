namespace Dxura.RP.Game;

/// <summary>
///     A component that manages the interaction and networking of prop entities, including cloud models.
/// </summary>
public partial class Prop() : BaseConstruct( ConstructType.Prop ), IDamageEvents
{
	[Property] private ModelRenderer ModelRenderer { get; set; } = null!;
	[Property] private ModelCollider ModelCollider { get; set; } = null!;

	private PropData _propData = new();

	private bool _hasSetModel;
	private bool _hasSetMaterial;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		OnUpdateFading();
	}

	public override void OnUnoccluded()
	{
		HandleModelAndMaterialLoading();
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		var oldPropData = oldData is PropData oldDataT ? oldDataT : default;
		var newPropData = newData is PropData newDataT ? newDataT : default;
		_propData = newPropData;

		if ( oldPropData.Model != newPropData.Model )
		{
			_hasSetModel = false;
			HandleModelAndMaterialLoading();
		}

		if ( oldPropData.Material != newPropData.Material )
		{
			_hasSetMaterial = false;
			HandleModelAndMaterialLoading();
		}

		if ( oldPropData.Tint != newPropData.Tint )
		{
			SetTint( newPropData.Tint );
		}

		if ( oldPropData.Scale != newPropData.Scale )
		{
			SetScale( newPropData.Scale );
		}

		SetNoCollide( newPropData.NoCollide, oldPropData.NoCollide != newPropData.NoCollide );

		// Handle fading door changes
		if ( HasFadingDoorDataChanged( oldPropData, newPropData ) )
		{
			UpdateFadingDoor( newPropData.FadingDoor, newPropData.FadingDoorDuration, newPropData.FadingDoorIsReversed );
		}

		if ( oldPropData.Friction != newPropData.Friction || oldPropData.Elasticity != newPropData.Elasticity )
		{
			SetPhysics( newPropData.Friction, newPropData.Elasticity );
		}
	}

	private void HandleModelAndMaterialLoading()
	{
		OcclusionSystem.Current.ForceCheckGameObject( GameObject );

		if ( GameObject.Tags.Has( Constants.OccludeTag ) && !Networking.IsHost && !IsPreview )
		{
			return;
		}

		if ( !_hasSetModel )
		{
			_hasSetModel = true;
			_ = SetModel( _propData.Model );
		}

		if ( !_hasSetMaterial )
		{
			_hasSetMaterial = true;
			_ = SetMaterial( _propData.Material );
		}
	}
}
