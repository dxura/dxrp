using Dxura.RP.Game.Tools;
using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Wire;

public class NotifierWireDefinition : ConstructDefinition<NotifierWire, NotifierWireData>
{
	public override ConstructType Type => ConstructType.NotifierWire;
	public override uint Limit => Config.Current.Game.NotiferWireLimit;

	public const uint MinNotifierTextLength = 1;
	public const uint MaxNotifierTextLength = 100;

	protected override ConstructDataValidationResult ValidateTyped( NotifierWireData data )
	{
		if ( string.IsNullOrWhiteSpace( data.Message ) )
		{
			return ConstructDataValidationResult.Failure( "Message cannot be empty." );
		}

		if ( data.Message.Length < MinNotifierTextLength || data.Message.Length > MaxNotifierTextLength )
		{
			return ConstructDataValidationResult.Failure( $"Message must be between {MinNotifierTextLength} and {MaxNotifierTextLength} characters." );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( NotifierWireData data, Vector3 position, Rotation rotation )
	{
		var gameObject = new GameObject( true, "Notifier" )
		{
			WorldPosition = position, WorldRotation = rotation
		};

		gameObject.Components.Create<NotifierWire>();

		var model = Model.Load( "models/props/office_fixtures/fire_alarm_a.vmdl" );

		var modelRenderer = gameObject.Components.Create<ModelRenderer>();
		modelRenderer.Model = model;
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		var collider = gameObject.Components.Create<ModelCollider>();
		collider.Model = model;

		gameObject.Tags.Add( Constants.ConstructTag, Constants.BuildInteractTag, Constants.OccludableTag );

		return gameObject;
	}
}
