using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Addons.Example;

/// <summary>Adds the Example page to the tab menu. Providers are discovered automatically.</summary>
public sealed class ExampleTabProvider : ITabMenuSectionProvider
{
	public void RegisterTabMenuSections( TabMenuSectionRegistry registry )
	{
		registry.Register( new TabMenuSectionDefinition
		{
			Id = "example.info",
			Label = "Example",
			Icon = "extension",
			PanelType = typeof(ExampleTab),
			Order = 70
		} );
	}
}
