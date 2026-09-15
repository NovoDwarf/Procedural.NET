using System.Collections.ObjectModel;
using System.Globalization;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Procedural.NET.Designer.ViewModels;

namespace Procedural.NET.Designer.Controls;

public sealed class GraphCanvas : Control
{
	public static readonly StyledProperty<ObservableCollection<GraphNodeViewModel>?> NodesProperty =
		AvaloniaProperty.Register<GraphCanvas, ObservableCollection<GraphNodeViewModel>?>(nameof(Nodes));

	public static readonly StyledProperty<ObservableCollection<GraphConnectionViewModel>?> ConnectionsProperty =
		AvaloniaProperty.Register<GraphCanvas, ObservableCollection<GraphConnectionViewModel>?>(nameof(Connections));

	public static readonly StyledProperty<GraphNodeViewModel?> SelectedNodeProperty =
		AvaloniaProperty.Register<GraphCanvas, GraphNodeViewModel?>(nameof(SelectedNode), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

	public static readonly StyledProperty<ICommand?> ConnectionRequestedCommandProperty =
		AvaloniaProperty.Register<GraphCanvas, ICommand?>(nameof(ConnectionRequestedCommand));

	private readonly Pen _gridPen = new(new SolidColorBrush(Color.FromRgb(42, 47, 56)), 1);
	private readonly Pen _connectionPen = new(new SolidColorBrush(Color.FromRgb(118, 169, 221)), 2);
	private readonly Pen _selectedPen = new(new SolidColorBrush(Color.FromRgb(127, 199, 255)), 2);
	private INotifyCollectionChanged? _observedNodes;
	private INotifyCollectionChanged? _observedConnections;
	private readonly List<GraphNodeViewModel> _observedNodeItems = [];
	private GraphNodeViewModel? _dragNode;
	private Point _dragOffset;
	private PortHit? _pendingPort;
	private PortHit? _pressedPort;
	private bool _isConnecting;
	private Point _connectionPoint;

	static GraphCanvas()
	{
		AffectsRender<GraphCanvas>(NodesProperty, ConnectionsProperty, SelectedNodeProperty);
	}

	public ObservableCollection<GraphNodeViewModel>? Nodes
	{
		get => GetValue(NodesProperty);
		set => SetValue(NodesProperty, value);
	}

	public ObservableCollection<GraphConnectionViewModel>? Connections
	{
		get => GetValue(ConnectionsProperty);
		set => SetValue(ConnectionsProperty, value);
	}

	public GraphNodeViewModel? SelectedNode
	{
		get => GetValue(SelectedNodeProperty);
		set => SetValue(SelectedNodeProperty, value);
	}

	public ICommand? ConnectionRequestedCommand
	{
		get => GetValue(ConnectionRequestedCommandProperty);
		set => SetValue(ConnectionRequestedCommandProperty, value);
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		DrawGrid(context);
		DrawConnections(context);
		DrawNodes(context);
		DrawPendingConnection(context);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == NodesProperty)
			ObserveCollection(ref _observedNodes, change.NewValue as INotifyCollectionChanged);

		if (change.Property == ConnectionsProperty)
			ObserveCollection(ref _observedConnections, change.NewValue as INotifyCollectionChanged);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		var point = e.GetPosition(this);
		var port = HitTestPort(point);
		if (port is not null)
		{
			SelectedNode = port.Node;
			if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
			{
				if (TryCompleteConnection(port))
				{
					InvalidateVisual();
					return;
				}

				_pendingPort = port;
				_pressedPort = port;
				_isConnecting = true;
				_connectionPoint = point;
				e.Pointer.Capture(this);
				InvalidateVisual();
				return;
			}
		}

		_dragNode = HitTestNode(point);
		SelectedNode = _dragNode;
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			_dragNode = null;
			InvalidateVisual();
			return;
		}

		if (_dragNode is not null)
		{
			_dragOffset = point - _dragNode.Position;
			e.Pointer.Capture(this);
		}

		InvalidateVisual();
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);

		if (_isConnecting)
		{
			_connectionPoint = e.GetPosition(this);
			InvalidateVisual();
			return;
		}

		if (_dragNode is null)
			return;

		var point = e.GetPosition(this);
		_dragNode.X = Math.Max(20, point.X - _dragOffset.X);
		_dragNode.Y = Math.Max(20, point.Y - _dragOffset.Y);
		InvalidateVisual();
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		if (_isConnecting)
		{
			var port = HitTestPort(e.GetPosition(this));
			if (port is not null && port != _pressedPort)
				TryCompleteConnection(port);

			_pressedPort = null;
			_isConnecting = false;
			e.Pointer.Capture(null);
			InvalidateVisual();
			return;
		}

		_dragNode = null;
		e.Pointer.Capture(null);
	}

	private void DrawGrid(DrawingContext context)
	{
		context.FillRectangle(new SolidColorBrush(Color.FromRgb(19, 21, 25)), Bounds);

		const double step = 32;
		for (var x = 0d; x < Bounds.Width; x += step)
			context.DrawLine(_gridPen, new Point(x, 0), new Point(x, Bounds.Height));

		for (var y = 0d; y < Bounds.Height; y += step)
			context.DrawLine(_gridPen, new Point(0, y), new Point(Bounds.Width, y));
	}

	private void DrawConnections(DrawingContext context)
	{
		if (Connections is null)
			return;

		foreach (var connection in Connections)
		{
			var start = OutputPortPoint(connection.Output, connection.OutputPortKey);
			var end = InputPortPoint(connection.Input, connection.InputPortKey);
			var geometry = new StreamGeometry();

			using (var ctx = geometry.Open())
			{
				ctx.BeginFigure(start, false);
				ctx.CubicBezierTo(
					new Point(start.X + 80, start.Y),
					new Point(end.X - 80, end.Y),
					end);
			}

			context.DrawGeometry(null, _connectionPen, geometry);
		}
	}

	private void DrawPendingConnection(DrawingContext context)
	{
		if (_pendingPort is null || !_isConnecting)
			return;

		var start = _pendingPort.IsOutput
			? OutputPortPoint(_pendingPort.Node, _pendingPort.PortKey)
			: InputPortPoint(_pendingPort.Node, _pendingPort.PortKey);
		var end = _isConnecting ? _connectionPoint : start;
		var geometry = new StreamGeometry();

		using (var ctx = geometry.Open())
		{
			ctx.BeginFigure(start, false);
			ctx.CubicBezierTo(
				new Point(start.X + (_pendingPort.IsOutput ? 80 : -80), start.Y),
				new Point(end.X + (_pendingPort.IsOutput ? -80 : 80), end.Y),
				end);
		}

		context.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(150, 210, 255)), 2.5), geometry);
	}

	private void DrawNodes(DrawingContext context)
	{
		if (Nodes is null)
			return;

		foreach (var node in Nodes)
		{
			var rect = NodeRect(node);
			var background = new SolidColorBrush(node.IsSelected ? Color.FromRgb(38, 45, 56) : Color.FromRgb(30, 34, 40));
			var borderPen = node.IsSelected ? _selectedPen : new Pen(new SolidColorBrush(Color.FromRgb(66, 72, 84)), 1);

			context.DrawRectangle(background, borderPen, rect, 6);
			context.DrawRectangle(node.ColorBrush, null, new Rect(rect.X, rect.Y, rect.Width, 5), 6, 6);

			context.DrawText(Text(node.DisplayName, 14, Color.FromRgb(240, 243, 246)), new Point(rect.X + 12, rect.Y + 16));
			context.DrawText(Text(node.StatusText, 11, Color.FromRgb(130, 210, 154)), new Point(rect.Right - 68, rect.Y + 18));
			PreviewPainter.Draw(context, node, new Rect(rect.X + 12, rect.Y + 42, rect.Width - 24, 88), false, highResolution: true);

			DrawPorts(context, node, rect);
		}
	}

	private static void DrawPorts(DrawingContext context, GraphNodeViewModel node, Rect rect)
	{
		var inputBrush = new SolidColorBrush(Color.FromRgb(126, 179, 238));
		var outputBrush = new SolidColorBrush(Color.FromRgb(236, 184, 92));
		var inputTextBrush = Color.FromRgb(176, 205, 238);
		var outputTextBrush = Color.FromRgb(236, 208, 160);

		for (var i = 0; i < node.Inputs.Count; i++)
		{
			var y = PortY(rect, i);
			context.DrawEllipse(inputBrush, null, new Point(rect.X, y), 6, 6);
			context.DrawText(Text(GraphNodeViewModel.Label(node.Inputs[i]), 10, inputTextBrush), new Point(rect.X + 10, y - 7));
		}

		for (var i = 0; i < node.Outputs.Count; i++)
		{
			var y = PortY(rect, i);
			var label = Text(GraphNodeViewModel.Label(node.Outputs[i]), 10, outputTextBrush);
			context.DrawEllipse(outputBrush, null, new Point(rect.Right, y), 6, 6);
			context.DrawText(label, new Point(rect.Right - Math.Min(88, label.Width) - 10, y - 7));
		}
	}

	private GraphNodeViewModel? HitTestNode(Point point)
	{
		if (Nodes is null)
			return null;

		for (var i = Nodes.Count - 1; i >= 0; i--)
		{
			if (NodeRect(Nodes[i]).Contains(point))
				return Nodes[i];
		}

		return null;
	}

	private PortHit? HitTestPort(Point point)
	{
		if (Nodes is null)
			return null;

		const double radius = 12;
		for (var nodeIndex = Nodes.Count - 1; nodeIndex >= 0; nodeIndex--)
		{
			var node = Nodes[nodeIndex];
			
			for (var i = 0; i < node.Outputs.Count; i++)
			{
				var port = node.Outputs[i];
				if (Distance(point, OutputPortPoint(node, port.Key)) <= radius)
					return new PortHit(node, port.Key, true);
			}

			for (var i = 0; i < node.Inputs.Count; i++)
			{
				var port = node.Inputs[i];
				if (Distance(point, InputPortPoint(node, port.Key)) <= radius)
					return new PortHit(node, port.Key, false);
			}
		}

		return null;
	}

	private bool TryCompleteConnection(PortHit target)
	{
		if (_pendingPort is null || _pendingPort.IsOutput == target.IsOutput)
			return false;

		if (_pendingPort.Node == target.Node)
		{
			_pendingPort = null;
			_pressedPort = null;
			_isConnecting = false;
			return true;
		}

		var request = _pendingPort.IsOutput
			? new GraphConnectionRequest(_pendingPort.Node, _pendingPort.PortKey, target.Node, target.PortKey)
			: new GraphConnectionRequest(target.Node, target.PortKey, _pendingPort.Node, _pendingPort.PortKey);

		if (ConnectionRequestedCommand?.CanExecute(request) != true)
			return false;

		ConnectionRequestedCommand.Execute(request);
		_pendingPort = null;
		_pressedPort = null;
		_isConnecting = false;
		return true;
	}

	private static Rect NodeRect(GraphNodeViewModel node)
	{
		var portRows = Math.Max(node.Inputs.Count, node.Outputs.Count);
		return new Rect(node.X, node.Y, 280, Math.Max(190, 148 + portRows * 20));
	}

	private static Point InputPortPoint(GraphNodeViewModel node, string portKey)
	{
		var index = Math.Max(0, node.Inputs.ToList().FindIndex(port => port.Key == portKey));
		var rect = NodeRect(node);
		return new Point(rect.X, PortY(rect, index));
	}

	private static Point OutputPortPoint(GraphNodeViewModel node, string portKey)
	{
		var index = Math.Max(0, node.Outputs.ToList().FindIndex(port => port.Key == portKey));
		var rect = NodeRect(node);
		return new Point(rect.Right, PortY(rect, index));
	}

	private static double PortY(Rect rect, int index) => rect.Y + 150 + index * 20;

	private static double Distance(Point a, Point b)
	{
		var dx = a.X - b.X;
		var dy = a.Y - b.Y;
		return Math.Sqrt(dx * dx + dy * dy);
	}

	private static FormattedText Text(string value, double size, Color color)
	{
		return new FormattedText(
			value,
			CultureInfo.CurrentCulture,
			FlowDirection.LeftToRight,
			Typeface.Default,
			size,
			new SolidColorBrush(color));
	}

	private void ObserveCollection(ref INotifyCollectionChanged? field, INotifyCollectionChanged? next)
	{
		if (field is not null)
			field.CollectionChanged -= OnCollectionChanged;

		field = next;

		if (field is not null)
			field.CollectionChanged += OnCollectionChanged;

		ObserveNodeItems();

		InvalidateVisual();
	}

	private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (sender == Nodes)
			ObserveNodeItems();

		InvalidateVisual();
	}

	private void ObserveNodeItems()
	{
		foreach (var node in _observedNodeItems)
			node.PropertyChanged -= OnNodePropertyChanged;

		_observedNodeItems.Clear();

		if (Nodes is null)
			return;

		foreach (var node in Nodes)
		{
			node.PropertyChanged += OnNodePropertyChanged;
			_observedNodeItems.Add(node);
		}
	}

	private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(GraphNodeViewModel.PreviewVersion) or nameof(GraphNodeViewModel.Position))
			InvalidateVisual();
	}

	private sealed record PortHit(GraphNodeViewModel Node, string PortKey, bool IsOutput);
}
