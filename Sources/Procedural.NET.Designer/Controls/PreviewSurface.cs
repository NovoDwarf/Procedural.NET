using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Procedural.NET.Designer.ViewModels;
using System.ComponentModel;

namespace Procedural.NET.Designer.Controls;

public sealed class PreviewSurface : Control
{
	public static readonly StyledProperty<GraphNodeViewModel?> NodeProperty =
		AvaloniaProperty.Register<PreviewSurface, GraphNodeViewModel?>(nameof(Node));

	public static readonly StyledProperty<bool> IsThreeDProperty =
		AvaloniaProperty.Register<PreviewSurface, bool>(nameof(IsThreeD));

	public static readonly StyledProperty<bool> HighResolutionProperty =
		AvaloniaProperty.Register<PreviewSurface, bool>(nameof(HighResolution));

	public static readonly StyledProperty<string> TextureModeProperty =
		AvaloniaProperty.Register<PreviewSurface, string>(nameof(TextureMode), PreviewPainter.PaletteGrayscale);

	private bool _isRotating;
	private Point _lastPoint;
	private double _yaw = -35;
	private double _pitch = 58;
	private double _zoom = 1;
	private GraphNodeViewModel? _observedNode;

	static PreviewSurface()
	{
		AffectsRender<PreviewSurface>(NodeProperty, IsThreeDProperty, HighResolutionProperty, TextureModeProperty);
	}

	public GraphNodeViewModel? Node
	{
		get => GetValue(NodeProperty);
		set => SetValue(NodeProperty, value);
	}

	public bool IsThreeD
	{
		get => GetValue(IsThreeDProperty);
		set => SetValue(IsThreeDProperty, value);
	}

	public bool HighResolution
	{
		get => GetValue(HighResolutionProperty);
		set => SetValue(HighResolutionProperty, value);
	}

	public string TextureMode
	{
		get => GetValue(TextureModeProperty);
		set => SetValue(TextureModeProperty, value);
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		PreviewPainter.Draw(context, Node, Bounds.Deflate(1), IsThreeD, _yaw, _pitch, _zoom, TextureMode, HighResolution);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == NodeProperty)
			ObserveNode(change.NewValue as GraphNodeViewModel);
	}

	private void ObserveNode(GraphNodeViewModel? node)
	{
		if (_observedNode is not null)
			_observedNode.PropertyChanged -= OnNodePropertyChanged;

		_observedNode = node;

		if (_observedNode is not null)
			_observedNode.PropertyChanged += OnNodePropertyChanged;

		InvalidateVisual();
	}

	private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(GraphNodeViewModel.PreviewVersion))
			InvalidateVisual();
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		if (!IsThreeD)
			return;

		_isRotating = true;
		_lastPoint = e.GetPosition(this);
		e.Pointer.Capture(this);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		if (!_isRotating || !IsThreeD)
			return;

		var point = e.GetPosition(this);
		var delta = point - _lastPoint;
		_lastPoint = point;
		_yaw += delta.X * 0.45;
		_pitch = Math.Clamp(_pitch - delta.Y * 0.35, 20, 82);
		InvalidateVisual();
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		_isRotating = false;
		e.Pointer.Capture(null);
	}

	protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
	{
		base.OnPointerWheelChanged(e);
		if (!IsThreeD)
			return;

		_zoom = Math.Clamp(_zoom + e.Delta.Y * 0.08, 0.55, 2.25);
		InvalidateVisual();
		e.Handled = true;
	}
}
