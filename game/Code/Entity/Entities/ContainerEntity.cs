using Sandbox.Diagnostics;
namespace Dxura.RP.Game.Entities;

[Title( "Container" )]
[Category( "Entities" )]
public sealed class ContainerEntity : BaseEntity, IDescription
{
	[Property]
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnQuantityChanged ) )]
	public int Quantity { get; set; }

	[Property]
	public required Resource ContainedResource { get; set; }

	[Property]
	public ContainerType ContainerType { get; set; } = ContainerType.Solid;

	[Property]
	public int DefaultQuantity { get; set; }

	[Property]
	public string Unit { get; set; } = "units";

	[Property]
	public bool DestroyOnEmpty { get; set; } = true;

	[Property]
	public Color? Tint { get; set; }

	[Property]
	private ModelRenderer ModelRenderer { get; set; } = null!;

	[Property]
	private TextRenderer? TextRenderer { get; set; }

	[Property]
	[Group( "Effects" )]
	private SoundEvent? UseSound { get; set; }

	[Property]
	private Decal? Decal { get; set; }

	public override string? DisplayName => string.Format( Language.GetPhrase( "entity.container.quantity" ),
		GetText( ContainedResource?.Name ),
		Quantity,
		GetText( Unit ) );

	public bool IsEmpty => Quantity <= 0;

	protected override void OnStart()
	{
		base.OnStart();

		UpdateState();
	}

	private void OnQuantityChanged( int oldValue, int newValue )
	{
		if ( newValue < oldValue )
		{
			UseSound.Play( WorldPosition );
		}

		if ( newValue <= 0 )
		{
			if ( DestroyOnEmpty )
			{
				GameObject.Destroy();
				return;
			}

			Quantity = 0;
		}

		UpdateText();
	}

	private void UpdateState()
	{
		if ( Networking.IsHost && Quantity <= 0 )
		{
			Quantity = DefaultQuantity;
		}

		if ( !ContainedResource.IsValid() )
		{
			return;
		}

		if ( Decal.IsValid() && ContainedResource.Icon != null )
		{
			var definition = new DecalDefinition
			{
				ColorTexture = ContainedResource.Icon
			};

			Decal.Decals = [definition];
		}

		if ( ModelRenderer.IsValid() && Tint.HasValue )
		{
			ModelRenderer.Tint = Tint.Value;
		}

		UpdateText();
	}

	private void UpdateText()
	{
		if ( TextRenderer.IsValid() )
		{
			TextRenderer.Text = $"{ContainedResource?.Identifier ?? "Unknown"} \n {Quantity} {Unit}";
		}
	}

	private static string GetText( string? text )
	{
		if ( string.IsNullOrWhiteSpace( text ) ) return string.Empty;
		return text.StartsWith( '#' ) ? Language.GetPhrase( text[1..] ) : text;
	}
}
