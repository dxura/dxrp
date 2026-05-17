using Dxura.RP.Shared;
using Dxura.RP.Game.Entities;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.scale.name", "#tool.scale.description", "#tool.group.miscellaneous" )]
public class ScaleTool : BaseTool
{
	[Property]
	[Range( 0.3f, 2f )]
	[Title( "Factor Scaling" )]
	[Description( "Multiply prop scale by a factor" )]
	public float ScaleFactor { get; set; } = 1.0f;

	[Property]
	[Title( "Axis Scaling " )]
	[Description( "When enabled, uses each input for X/Y/Z values" )]

	public bool UseIndividualScaling { get; set; } = false;

	[Property]
	[Title( "X (Length) Scale" )]
	public string XScaleText { get; set; } = "1.0";

	[Property]
	[Title( "Y (Height) Scale" )]
	public string YScaleText { get; set; } = "1.0";

	[Property]
	[Title( "Z (Width) Scale" )]
	[Description( "Enter scale value (e.g., 1.5, 1, 0.5)" )]
	public string ZScaleText { get; set; } = "1.0";

	public override string Attack1Control => UseIndividualScaling ? "#tool.scale.attack1_axis" : "#tool.scale.attack1";
	public override string Attack2Control => "#tool.scale.attack2";
	public override string ReloadControl => "#tool.scale.reload";

	private bool TryParseScaleValues( out Vector3 scaleValues )
	{
		scaleValues = Vector3.One;

		if ( !float.TryParse( XScaleText, out var x ) ||
		     !float.TryParse( YScaleText, out var y ) ||
		     !float.TryParse( ZScaleText, out var z ) )
		{
			return false;
		}

		// Validate scale limits
		if ( x < PropDefinition.MinPropScale || x > PropDefinition.MaxPropScale ||
		     y < PropDefinition.MinPropScale || y > PropDefinition.MaxPropScale ||
		     z < PropDefinition.MinPropScale || z > PropDefinition.MaxPropScale )
		{
			Notify.Error( $"Scale must be between {PropDefinition.MinPropScale} and {PropDefinition.MaxPropScale}" );
			return false;
		}

		scaleValues = new Vector3( x, y, z );
		return true;
	}

	public override void PrimaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:scale:use", Config.Current.Game.ScaleCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit )
		{
			return;
		}

		if ( !tr.Body.IsValid() )
		{
			return;
		}

		// Check for scalable components (unified - entity or construct)
		var targetObject = tr.GameObject.Root;

		Vector3 scaleValues;
		if ( UseIndividualScaling )
		{
			// Parse the text input values
			if ( !TryParseScaleValues( out scaleValues ) )
			{
				return;
			}
		}
		else
		{
			// Validate factor scaling limits
			if ( ScaleFactor is < PropDefinition.MinPropScale or > PropDefinition.MaxPropScale )
			{
				Notify.Error( $"Scale must be between {PropDefinition.MinPropScale} and {PropDefinition.MaxPropScale}" );
				return;
			}

			// Apply factor scaling
			scaleValues = Vector3.One * ScaleFactor;
		}

		if ( !Scale( targetObject, scaleValues ) )
		{
			return;
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	public override void SecondaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:scale:use", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit )
		{
			return;
		}

		if ( !tr.Body.IsValid() )
		{
			return;
		}

		// Get the target object
		var targetObject = tr.GameObject.Root;

		// Try to get scale from prop
		var prop = targetObject.GetComponent<Prop>();
		if ( prop.IsValid() && prop.Data is PropData propData )
		{
			CopyScaleFromVector( propData.Scale );
			Notify.Info( "#tool.scale.copied" );
			Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
			return;
		}

		// Try to get scale from entity
		var entity = targetObject.GetComponent<BaseEntity>();
		if ( entity.IsValid() && entity.CanScale( Player.Local ) )
		{
			CopyScaleFromVector( targetObject.WorldScale );
			Notify.Info( "#tool.scale.copied" );
			Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
			return;
		}

		Notify.Warn( "#tool.scale.cannot_copy" );
	}

	public override void ReloadUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:scale:use", Config.Current.Game.ScaleCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit )
		{
			return;
		}

		if ( !tr.Body.IsValid() )
		{
			return;
		}

		// Check for scalable components (unified - entity or construct)
		var targetObject = tr.GameObject.Root;

		// Reset scale to 1.0x
		if ( !Scale( targetObject, Vector3.One ) )
		{
			return;
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	private void CopyScaleFromVector( Vector3 scale )
	{
		// Check if this is a uniform scale (all axes are equal)
		if ( scale.x == scale.y && scale.y == scale.z )
		{
			// Set factor scaling mode
			UseIndividualScaling = false;
			ScaleFactor = scale.x;
		}
		else
		{
			// Set axis scaling mode
			UseIndividualScaling = true;
			XScaleText = scale.x.ToString( "F2" );
			YScaleText = scale.y.ToString( "F2" );
			ZScaleText = scale.z.ToString( "F2" );
		}
	}

	private bool Scale( GameObject target, Vector3 scale )
	{
		if ( !GameManager.Instance.RequestOwnership( target ) )
		{
			Notify.Error( "#generic.permission" );
			return false;
		}

		// Do prop
		var prop = target.GetComponent<Prop>();
		if ( prop.IsValid() )
		{
			if ( prop.Data is not PropData propData )
			{
				return false;
			}

			var newData = propData with
			{
				Scale = scale
			};

			Construct.Current.UpdateConstructPlayer( ConstructType.Prop, newData, target );

			return true;
		}

		// Do entity
		var entity = target.GetComponent<BaseEntity>();
		if ( entity.IsValid() && entity.CanScale( Player.Local ) )
		{
			GameManager.Instance.ScaleEntityHost( target, scale );
			return true;
		}

		return false;
	}

}
