using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;
namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.name", "#tool.wire.description", "#tool.group.core", Category = ToolCategory.Wire )]
public class WireTool : BaseTool
{
	[Property]
	[Title( "Color" )]
	private Color Color { get; set; } = Color.Black;

	[Property] [Title( "Thickness" )]
	[Range( Wire.Wire.MinWireThickness, Wire.Wire.MaxWireThickness )] [Step( 0.05f )]
	private float Thickness { get; set; } = Wire.Wire.MaxWireThickness;

	[Property] [Title( "Opacity" )] [Range( Wire.Wire.MinWireOpacity, Wire.Wire.MaxWireOpacity )] [Step( 0.05f )]
	private float Opacity { get; set; } = Wire.Wire.MaxWireOpacity;

	public static bool IsDeployed { get; private set; }

	public override string Attack1Control => "#tool.wire.attack1";
	public override string Attack2Control => "#tool.wire.attack2";
	public override string ReloadControl => "#tool.wire.reload";

	private IWireComponent? _selectedWireComponent;
	private string? _selectedWireInput;
	private int _selectedInputIndex = 0;
	private readonly Stack<Vector3> _anchors = new();

	// Pre-selection based on what you're looking at
	private IWireComponent? _hoveredWireComponent;
	private int _hoveredOutputIndex = 0;
	private int _hoveredInputIndex = 0;

	// Highlight wire rendering
	private static readonly Color HighlightColor = new( 0.043f, 0.682f, 0.859f ); // Cyan highlight
	private readonly List<SceneLineObject> _highlightWireLines = new();

	// Track the currently highlighted connections so Wire.cs can skip rendering them
	public static HashSet<Guid> HighlightedConnectionIds { get; private set; } = new();

	// Public properties for UI access
	public IWireComponent? SelectedWireComponent => _selectedWireComponent;

	public IWireComponent? HoveredWireComponent => _hoveredWireComponent;
	public int HoveredOutputIndex => _hoveredOutputIndex;
	public int HoveredInputIndex => _hoveredInputIndex;

	/// <summary>
	/// Gets all wire connections for the currently hovered input (if any).
	/// Used for highlighting connected wires when scrolling through input ports.
	/// </summary>
	private List<WireConnection> GetHighlightedConnections()
	{
		// Only highlight when in input selection mode (before selecting the first component)
		if ( _selectedWireComponent != null || _hoveredWireComponent == null )
		{
			return [];
		}

		var inputs = _hoveredWireComponent.GetInputPorts().ToList();
		if ( _hoveredInputIndex < 0 || _hoveredInputIndex >= inputs.Count )
		{
			return [];
		}

		var inputPort = inputs[_hoveredInputIndex];
		return Wire.Wire.Current.GetConnections( _hoveredWireComponent )
			.Where( c => c.Target == _hoveredWireComponent && c.InputId == inputPort.Id && c.Source != null )
			.ToList();
	}

	public override void OnToolFixedUpdate()
	{
		if ( !Player.Local.IsValid() )
		{
			return;
		}

		IsDeployed = true;

		// Update pre-selection based on what we're looking at
		UpdateHoveredComponent();

		var isLookingAtWire = _hoveredWireComponent != null;
		var canScroll = CanScrollOnHoveredComponent();

		// Don't allow switching tools while looking at a wire component that can be scrolled
		Player.Local.CantSwitch = isLookingAtWire && canScroll;

		if ( canScroll )
		{
			// Handle scroll wheel for port selection
			HandleScrollSelection();
		}

		// Render highlight wire for connected inputs
		RenderHighlightWire();
	}
	public override void PrimaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:wire:connect", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		var go = tr.GameObject.Root;
		var wireComponent = go.GetComponent<IWireComponent>();

		if ( wireComponent == null || !wireComponent.GameObject.IsValid() )
		{
			Notify.Error( "#tool.wire.invalid" );
			return;
		}

		if ( !GameUtils.HasPermission( Connection.Local, wireComponent.GameObject ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		// If no selection, select this component's input
		if ( _selectedWireComponent == null )
		{
			var inputs = wireComponent.GetInputPorts().ToList();
			if ( inputs.Count == 0 )
			{
				Notify.Error( "#tool.wire.no_inputs" );
				return;
			}

			_selectedWireComponent = wireComponent;
			_selectedInputIndex = _hoveredInputIndex;
			_selectedWireInput = inputs[_selectedInputIndex].Id;
			_anchors.Clear();

			Notify.Success( $"Selected input: {_selectedWireInput}" );
			Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
			return;
		}

		// Connect from this component's output to the selected input
		var outputs = wireComponent.GetOutputPorts().ToList();
		if ( outputs.Count == 0 )
		{
			Notify.Error( "#tool.wire.no_outputs" );
			return;
		}

		// Use the selected output from hovering
		var targetOutputIndex = _hoveredOutputIndex;
		if ( targetOutputIndex >= outputs.Count )
		{
			targetOutputIndex = 0;
		}
		var targetOutput = outputs[targetOutputIndex];

		// Make the connection
		if ( _selectedWireInput != null )
		{
			Wire.Wire.Current.RequestConnect(
				wireComponent, targetOutput.Id,
				_selectedWireComponent, _selectedWireInput
				, _anchors.ToList(), Color, Thickness, Opacity );
		}
		else
		{
			Notify.Error( "#tool.wire.connection_failed" );
		}

		Clear();

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	public override void SecondaryUseStart()
	{
		if ( _selectedWireComponent == null || _selectedWireInput == null )
		{
			Notify.Error( "#tool.wire.invalid" );
			return;
		}

		if ( _anchors.Count > Wire.Wire.MaxWireAnchorCount )
		{
			Notify.Error( "#tool.wire.too_many_anchors" );
			return;
		}

		var tr = PerformEyeTrace();
		if ( !tr.Hit )
		{
			return;
		}

		_anchors.Push( tr.HitPosition + tr.Normal * 2.0f ); // Add offset to make it float above the surface
		Notify.Success( $"Added anchor point floating above {tr.HitPosition}" );
		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	public override void ReloadUseStart()
	{
		base.ReloadUseStart();

		if ( Cooldown.Current.CheckAndStartCooldown( "tool:wire:reset", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		// If hovering over an input that is connected, remove all connections to that input
		if ( _hoveredWireComponent != null )
		{
			var inputs = _hoveredWireComponent.GetInputPorts().ToList();
			if ( _hoveredInputIndex < inputs.Count )
			{
				var inputPort = inputs[_hoveredInputIndex];

				var hasConnections = Wire.Wire.Current.GetConnections( _hoveredWireComponent )
					.Any( c => c.InputId == inputPort.Id && c.Source != null && c.Target != null );

				if ( hasConnections )
				{
					Wire.Wire.Current.RequestDisconnectAll( _hoveredWireComponent, inputPort.Id );
					return;
				}
			}
		}

		Clear();
		Notify.Success( "#tool.wire.reset" );
	}

	public override void OnUnequip()
	{
		base.OnUnequip();

		if ( Player.Local.IsValid() )
		{
			Player.Local.CantSwitch = false; // Reset the switch lock
		}

		// Clean up highlight wires
		CleanupHighlightWires();

		IsDeployed = false;
	}

	private void Clear()
	{
		// Reset for next connection
		_selectedWireComponent = null;
		_selectedWireInput = null;
		_anchors.Clear();
		_hoveredInputIndex = 0;
		_hoveredOutputIndex = 0;
	}

	private void UpdateHoveredComponent()
	{
		var tr = PerformEyeTrace();
		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			_hoveredWireComponent = null;
			return;
		}

		var wireComponent = tr.GameObject.Root.GetComponent<IWireComponent>();
		if ( wireComponent == null )
		{
			_hoveredWireComponent = null;
			return;
		}

		// If this is a new component we're hovering over, reset indices
		if ( _hoveredWireComponent != wireComponent )
		{
			_hoveredWireComponent = wireComponent;
			_hoveredOutputIndex = 0;
			_hoveredInputIndex = 0;
		}
	}

	private bool CanScrollOnHoveredComponent()
	{
		if ( _hoveredWireComponent == null )
		{
			return false;
		}

		// If no selection, check if inputs can be scrolled
		if ( _selectedWireComponent == null )
		{
			var inputs = _hoveredWireComponent.GetInputPorts().ToList();
			return inputs.Count > 1;
		}
		else
		{
			var outputs = _hoveredWireComponent.GetOutputPorts().ToList();
			return outputs.Count > 1;
		}
	}

	private void HandleScrollSelection()
	{
		if ( Input.MouseWheel.y == 0 || _hoveredWireComponent == null )
		{
			return;
		}

		// If no selection, scroll through inputs of hovered component
		if ( _selectedWireComponent == null )
		{
			ScrollHoveredInputSelection( Input.MouseWheel.y > 0 );
		}
		else
		{
			ScrollHoveredOutputSelection( Input.MouseWheel.y > 0 );
		}

		// Consume the scroll input
		Input.MouseWheel = Vector2.Zero;
	}

	private void ScrollHoveredOutputSelection( bool scrollUp )
	{
		if ( _hoveredWireComponent == null )
		{
			return;
		}

		var outputs = _hoveredWireComponent.GetOutputPorts().ToList();
		if ( outputs.Count <= 1 )
		{
			return;
		}

		if ( scrollUp )
		{
			_hoveredOutputIndex = (_hoveredOutputIndex - 1 + outputs.Count) % outputs.Count;
		}
		else
		{
			_hoveredOutputIndex = (_hoveredOutputIndex + 1) % outputs.Count;
		}

		Sound.Play( "pop" );
	}

	private void ScrollHoveredInputSelection( bool scrollUp )
	{
		if ( _hoveredWireComponent == null )
		{
			return;
		}

		var inputs = _hoveredWireComponent.GetInputPorts().ToList();
		if ( inputs.Count <= 1 )
		{
			return;
		}

		if ( scrollUp )
		{
			_hoveredInputIndex = (_hoveredInputIndex - 1 + inputs.Count) % inputs.Count;
		}
		else
		{
			_hoveredInputIndex = (_hoveredInputIndex + 1) % inputs.Count;
		}

		Sound.Play( "pop" );
	}

	private void RenderHighlightWire()
	{
		var highlightedConnections = GetHighlightedConnections();

		if ( highlightedConnections.Count == 0 )
		{
			CleanupHighlightWires();
			return;
		}

		// Track which connections we're highlighting so Wire.cs can skip rendering them
		HighlightedConnectionIds.Clear();

		var sceneWorld = Sandbox.Game.ActiveScene?.SceneWorld;
		if ( sceneWorld == null )
		{
			CleanupHighlightWires();
			return;
		}

		var activeCount = 0;

		foreach ( var connection in highlightedConnections )
		{
			if ( connection.Source == null || connection.Target == null ||
			     !connection.Source.GameObject.IsValid() || !connection.Target.GameObject.IsValid() )
			{
				continue;
			}

			HighlightedConnectionIds.Add( connection.Id );

			// Get or create a line object from pool
			SceneLineObject line;
			if ( activeCount < _highlightWireLines.Count )
			{
				line = _highlightWireLines[activeCount];
			}
			else
			{
				line = new SceneLineObject( sceneWorld )
				{
					EndCap = SceneLineObject.CapStyle.Rounded, StartCap = SceneLineObject.CapStyle.Rounded, Lighting = false, Material = Material.Load( "materials/default/default_line.vmat" )
				};
				_highlightWireLines.Add( line );
			}

			activeCount++;

			var fromPosition = connection.Source.GetPortPosition();
			var toPosition = connection.Target.GetPortPosition();

			Wire.Wire.RenderWireLine( line, fromPosition, toPosition, connection.Anchors, HighlightColor, connection.Thickness );
		}

		// Clean up unused lines from pool
		for ( var i = _highlightWireLines.Count - 1; i >= activeCount; i-- )
		{
			_highlightWireLines[i].Delete();
			_highlightWireLines.RemoveAt( i );
		}
	}

	private void CleanupHighlightWires()
	{
		foreach ( var line in _highlightWireLines )
		{
			line.Delete();
		}
		_highlightWireLines.Clear();
		HighlightedConnectionIds.Clear();
	}
}
