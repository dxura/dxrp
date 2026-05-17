using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.material.name", "#tool.material.description", "#tool.group.render" )]
public class MaterialTool : BaseTool
{
	[Property]
	[MaterialProperty]
	public string SelectedMaterial { get; set; } = "";

	[Property]
	public List<string> MaterialOptions { get; set; } = new();

	public override string Attack1Control => "#tool.material.attack1";
	public override string Attack2Control => "#tool.material.attack2";
	public override string ReloadControl => "#tool.material.reload";

	public event Action? OnMaterialsRefreshed;

	public override void OnEquip()
	{
		base.OnEquip();
		RefreshMaterials();
		ToolMenu.Instance?.UpdateInspector();
		OnMaterialsRefreshed?.Invoke(); // Force refresh to ensure MaterialPicker updates
	}

	private void RefreshMaterials()
	{
		MaterialOptions.Clear();

		// Use Constants.MaterialWhitelist instead of scanning filesystem
		foreach ( var materialPath in Config.Current.Game.MaterialWhitelist )
		{
			MaterialOptions.Add( materialPath );
		}

		if ( MaterialOptions.Count > 0 && string.IsNullOrWhiteSpace( SelectedMaterial ) )
		{
			SelectedMaterial = MaterialOptions[0];
		}
		else if ( MaterialOptions.Count == 0 )
		{
			SelectedMaterial = "";
		}

		OnMaterialsRefreshed?.Invoke();
	}

	public override void PrimaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:material:use", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		var go = tr.GameObject.Root;

		if ( !GameUtils.HasPermission( Player.Local.SteamId, go ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		if ( string.IsNullOrWhiteSpace( SelectedMaterial ) )
		{
			Notify.Error( "#tool.material.no_selection" );
			return;
		}

		var prop = go.GetComponent<Prop>();
		if ( !prop.IsValid() )
		{
			return;
		}

		if ( prop.Data is PropData propData )
		{
			var newData = propData with
			{
				Material = SelectedMaterial
			};
			Construct.Current.UpdateConstructPlayer( prop.Type, newData, go );
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	public override void SecondaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:material:use", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		var go = tr.GameObject.Root;

		var prop = go.GetComponent<Prop>();

		if ( !prop.IsValid() )
		{
			return;
		}

		if ( prop.Data is PropData propData && !string.IsNullOrWhiteSpace( propData.Material ) )
		{
			SelectedMaterial = propData.Material;
			Notify.Success( "#tool.material.copied" );
			Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );

			if ( !MaterialOptions.Contains( propData.Material ) )
			{
				MaterialOptions.Add( propData.Material );
				OnMaterialsRefreshed?.Invoke();
			}
		}
	}

	public override void ReloadUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:material:use", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();
		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		var root = tr.GameObject.Root;

		if ( !GameUtils.HasPermission( Player.Local.SteamId, root ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		var prop = root.GetComponent<Prop>();
		if ( !prop.IsValid() )
		{
			return;
		}

		if ( prop.Data is PropData propData )
		{
			var newData = propData with
			{
				Material = ""
			};
			Construct.Current.UpdateConstructPlayer( prop.Type, newData, root );
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		Notify.Success( "#tool.material.reset" );
	}
}
