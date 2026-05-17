using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.permanent.name", "#tool.permanent.description", "#tool.group.miscellaneous", RequiredPermissions = [Permission.Permanent] )]
public class PermanentTool : BaseTool
{
	public const int MinRange = 25;
	public const int MaxRange = 500;

	[Property]
	[Title( "Range" )]
	[Range( MinRange, MaxRange )]
	[Step( 5 )]
	public int Radius { get; set; } = 25;

	public override string Attack1Control => "#tool.permanent.attack1";
	public override string Attack2Control => "#tool.permanent.attack2";

	private static readonly Color PermanentOutlineColor = new( 0.1f, 0.8f, 0.2f );
	private readonly HashSet<GameObject> _outlined = new();
	private TimeSince _lastRefresh;

	public override void OnUnequip()
	{
		ClearOutlines();
		base.OnUnequip();
	}

	private void ClearOutlines()
	{
		foreach ( var go in _outlined )
		{
			if ( !go.IsValid() )
			{
				continue;
			}

			var outline = go.GetComponent<HighlightOutline>();
			if ( outline.IsValid() )
			{
				outline.Destroy();
			}
		}
		_outlined.Clear();
	}

	public override void OnToolUpdate()
	{
		base.OnToolUpdate();

		if ( !RankSystem.HasLocalPermission( Permission.Permanent ) )
		{
			return;
		}

		if ( _lastRefresh > 1f )
		{
			RefreshOutlines();
		}

		var tr = PerformEyeTrace();
		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		if ( !tr.GameObject.Tags.HasAny( Constants.ConstructTag, Constants.EntityTag ) )
		{
			return;
		}

		var root = tr.GameObject.Root;
		var owned = root.GetComponent<IOwned>();
		var isPermanent = owned is { Owner: 0 };
		Gizmo.Draw.Color = isPermanent ? Color.Green : Color.Yellow;
		Gizmo.Draw.LineSphere( root.WorldPosition, Radius );
	}

	private void RefreshOutlines()
	{
		_lastRefresh = 0;

		var currentPermanent = new HashSet<GameObject>();

		foreach ( var construct in Sandbox.Game.ActiveScene.GetAll<IConstruct>() )
		{
			if ( construct.IsValid() && !construct.IsPreview && construct.Owner == 0 )
			{
				currentPermanent.Add( construct.GameObject.Root );
			}
		}

		foreach ( var entity in Sandbox.Game.ActiveScene.GetAllComponents<BaseEntity>() )
		{
			if ( entity.IsValid() && entity.Owner == 0 )
			{
				currentPermanent.Add( entity.GameObject.Root );
			}
		}

		// Remove outlines from objects that are no longer permanent
		foreach ( var go in _outlined )
		{
			if ( currentPermanent.Contains( go ) )
			{
				continue;
			}

			if ( !go.IsValid() )
			{
				continue;
			}

			var outline = go.GetComponent<HighlightOutline>();
			if ( outline.IsValid() )
			{
				outline.Destroy();
			}
		}

		// Add outlines to new permanent objects
		foreach ( var go in currentPermanent )
		{
			if ( _outlined.Contains( go ) )
			{
				continue;
			}

			var outline = go.GetOrAddComponent<HighlightOutline>();
			outline.Color = PermanentOutlineColor;
			outline.Width = 0.25f;
			outline.InsideColor = Color.Transparent;
			outline.ObscuredColor = PermanentOutlineColor.WithAlpha( 0.1f );
		}

		_outlined.Clear();
		foreach ( var go in currentPermanent )
		{
			_outlined.Add( go );
		}
	}

	public override void PrimaryUseStart()
	{
		if ( !RankSystem.HasLocalPermission( Permission.Permanent ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		var tr = PerformEyeTrace();
		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		if ( !tr.GameObject.Tags.HasAny( Constants.ConstructTag, Constants.EntityTag ) )
		{
			return;
		}

		GameManager.Instance.SetPermanentHost( tr.GameObject.Root.WorldPosition, Radius );
		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		_lastRefresh = 2f;
	}

	public override void SecondaryUseStart()
	{
		if ( !RankSystem.HasLocalPermission( Permission.Permanent ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		var tr = PerformEyeTrace();
		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		var root = tr.GameObject.Root;
		if ( !root.Tags.HasAny( Constants.ConstructTag, Constants.EntityTag ) )
		{
			return;
		}

		GameManager.Instance.ClearPermanentHost( root.WorldPosition, Radius );
		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		_lastRefresh = 2f;
	}

}
