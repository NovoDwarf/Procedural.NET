using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using NovoDwarf.Primitives.Models.Float2;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Execution.Interfaces;
using Procedural.NET.Designer.Compute;
using Procedural.NET.Designer.Utilities;
using Procedural.NET.Nodes;
using Procedural.NET.Nodes.Spatial;
using Procedural.NET.Nodes.Terrain.Display;
using Procedural.NET.Storage;

namespace Procedural.NET.Designer.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
	private readonly GraphDocument _document = new();
	private readonly GraphFactory _factory;
	private readonly GraphSessionService _sessionService;
	private readonly Dictionary<Guid, GraphNodeViewModel> _nodesById = [];
	
	private GraphNode? _copiedNode;
	private GraphSession? _savedSession;
	private GraphSessionTabViewModel? _selectedSession;
	private string _selectedComputeBackend;

	public MainWindowViewModel()
	{
		var executors = CreateExecutors();
		
		_factory = new GraphFactory(executors);
		_sessionService = new GraphSessionService(_factory);
		_selectedComputeBackend = ComputeBackends[0];
		
		Sessions.Add(new GraphSessionTabViewModel("Session 1", _document));
		
		_selectedSession = Sessions[0];

		
		Catalog =
		[
			.. new GraphNodeCatalog(executors).Executors
			                         .Where(static executor => executor.GroupKey != NodeGroupKeys.Display)
			                         .Select(CreateCatalogItem)
		];
		
		Library = BuildLibrary(Catalog);
		
		New();
	}

	public ObservableCollection<CatalogNodeViewModel> Catalog { get; }
	public ObservableCollection<LibraryItemViewModel> Library { get; }
	public ObservableCollection<GraphNodeViewModel> Nodes { get; } = [];
	public ObservableCollection<GraphConnectionViewModel> Connections { get; } = [];
	public ObservableCollection<InspectorParameterViewModel> InspectorParameters { get; } = [];
	public ObservableCollection<GraphSessionTabViewModel> Sessions { get; } = [];
	
	public IReadOnlyList<string> ComputeBackends { get; } = ["CPU", "Slang GPU (planned)"];
	public IReadOnlyList<string> PreviewTextureModes { get; } = ["Grayscale", "Gradient", "Terrain", "Water"];
	
	public LibraryItemViewModel? SelectedLibraryItem
	{
		get;
		set
		{
			if (!SetProperty(ref field, value) || value is null)
				return;

			if (value.CatalogItem is not null)
				AddNode(value.CatalogItem);
			else if (value.Kind == LibraryItemKind.Session)
				Load();

			field = null;
			OnPropertyChanged();
		}
	}

	public GraphNodeViewModel? SelectedNode
	{
		get;
		set
		{
			if (field == value)
				return;

			field?.IsSelected = false;
			field = value;
			field?.IsSelected = true;

			OnPropertyChanged();
			RefreshInspector();
		}
	}

	public GraphNodeViewModel? ActivePreviewNode => SelectedNode ?? Nodes.FirstOrDefault();

	public GraphSessionTabViewModel? SelectedSession
	{
		get => _selectedSession;
		set => SetProperty(ref _selectedSession, value);
	}

	public string SelectedComputeBackend
	{
		get => _selectedComputeBackend;
		set => SetProperty(ref _selectedComputeBackend, value);
	}

	public string StatusText
	{
		get;
		set => SetProperty(ref field, value);
	} = "Готово";

	public string PreviewText
	{
		get;
		set => SetProperty(ref field, value);
	} = "Нет узла";

	public string PreviewBadge
	{
		get;
		set => SetProperty(ref field, value);
	} = "2D";

	public string SelectedPreviewTextureMode
	{
		get;
		set => SetProperty(ref field, value);
	} = "Grayscale";

	public string InspectorTitle => SelectedNode?.DisplayName ?? "\u041d\u0435\u0442 \u0443\u0437\u043b\u0430";
	public string InspectorSubtitle => SelectedNode?.Model.Executor.Key ?? "";
	public string InspectorPreviewBadge => ActivePreviewNode?.PreviewMode ?? "2D";
	public string SelectedNodeStatus => SelectedNode?.StatusText ?? "\u041e\u0436\u0438\u0434\u0430\u043d\u0438\u0435";

	[RelayCommand]
	private void New()
	{
		_document.Clear();
		_nodesById.Clear();
		Nodes.Clear();
		Connections.Clear();
		
		SelectedNode = null;

		var noiseCatalog = FindCatalogItem(NodeKeys.NoiseValue);
		var rampCatalog = FindCatalogItem(NodeKeys.ColorRamp);
		
		if (noiseCatalog is null || rampCatalog is null)
		{
			StatusText = "Пустой граф";
			RefreshInspector();
			return;
		}

		var source = AddNode(noiseCatalog, 120, 120);
		var ramp = AddNode(rampCatalog, 420, 130);

		Connect(source, PortKeys.C, ramp, PortKeys.Source);

		SelectedNode = source;
		StatusText = "Граф создан";
	}

	[RelayCommand]
	private void Save()
	{
		_savedSession = _sessionService.Capture(_document, null);
		RefreshSessionLibrary();
		StatusText = $"Сохранено: {_savedSession.Nodes.Count}";
	}

	[RelayCommand]
	private void Load()
	{
		if (_savedSession is null)
		{
			StatusText = "Нет сессии";
			return;
		}

		_sessionService.Apply(_document, _savedSession);
		
		RebuildViewModels();
		StatusText = $"Загружено: {_savedSession.Nodes.Count}";
	}

	[RelayCommand]
	private void Delete()
	{
		if (SelectedNode is null)
			return;

		var next = Nodes.FirstOrDefault(node => node != SelectedNode);
		_document.RemoveNodes([SelectedNode.Model]);
		
		RebuildViewModels();
		
		SelectedNode = next is null ? Nodes.FirstOrDefault() : Nodes.FirstOrDefault(node => node.Id == next.Id);
		StatusText = "Удалено";
	}

	[RelayCommand]
	private void Copy()
	{
		if (SelectedNode is null)
			return;

		_copiedNode = SelectedNode.Model;
		StatusText = "Скопировано";
	}

	[RelayCommand]
	private void Paste()
	{
		if (_copiedNode is null)
			return;

		var catalog = FindCatalogItem(_copiedNode.Executor.Key);
		if (catalog is null)
			return;

		var node = AddNode(catalog, _copiedNode.Position.X + 36, _copiedNode.Position.Y + 36);
		node.Model.CopyFrom(_copiedNode);
		
		StatusText = "\u0412\u0441\u0442\u0430\u0432\u043b\u0435\u043d\u043e";
		RefreshInspector();
	}

	[RelayCommand]
	private void Duplicate()
	{
		Copy();
		Paste();
		
		StatusText = "Дублировано";
	}

	[RelayCommand]
	private void ConnectionRequested(object? parameter)
	{
		if (parameter is not GraphConnectionRequest request)
			return;

		StatusText = Connect(request.Output, request.OutputPortKey, request.Input, request.InputPortKey)
			? "Соединено"
			: "Порты не совместимы";
	}

	private GraphNodeViewModel AddNode(CatalogNodeViewModel catalogItem, double? x = null, double? y = null)
	{
		var position = new Float2(
			(float)(x ?? 180 + Nodes.Count * 48),
			(float)(y ?? 120 + Nodes.Count * 36));
		var node = _factory.CreateNode(catalogItem.ExecutorKey, position);
		_document.AddNode(node);

		var viewModel = new GraphNodeViewModel(node, catalogItem.Name, ColorUtils.BrushColor(catalogItem.ColorBrush));
		Nodes.Add(viewModel);
		_nodesById[node.Id] = viewModel;
		SelectedNode = viewModel;
		StatusText = $"Добавлено: {catalogItem.Name}";
		return viewModel;
	}

	private bool Connect(GraphNodeViewModel output, string outputPortKey, GraphNodeViewModel input, string inputPortKey)
	{
		if (!_document.TryConnect(output.Id, outputPortKey, input.Id, inputPortKey))
			return false;

		Connections.Add(new GraphConnectionViewModel(output, outputPortKey, input, inputPortKey));
		return true;
	}

	private void RebuildViewModels()
	{
		_nodesById.Clear();
		Nodes.Clear();
		Connections.Clear();

		foreach (var node in _document.Nodes)
		{
			var catalog = Catalog.FirstOrDefault(item => item.ExecutorKey == node.Executor.Key);
			var viewModel = new GraphNodeViewModel(
				node,
				catalog?.Name ?? GraphNodeViewModel.Label(node.Executor.LocalizationKey),
				catalog is null ? Colors.Gray : ColorUtils.BrushColor(catalog.ColorBrush));
			Nodes.Add(viewModel);
			_nodesById[node.Id] = viewModel;
		}

		foreach (var (input, output) in _document.GetAllConnections())
		{
			if (!_nodesById.TryGetValue(input.NodeId, out var inputNode) ||
			    !_nodesById.TryGetValue(output.NodeId, out var outputNode))
				continue;

			Connections.Add(new GraphConnectionViewModel(outputNode, output.PortKey, inputNode, input.PortKey));
		}

		SelectedNode = Nodes.FirstOrDefault();
		OnPropertyChanged(nameof(ActivePreviewNode));
	}

	private void RefreshInspector()
	{
		InspectorParameters.Clear();
		var previewNode = ActivePreviewNode;

		if (SelectedNode is null)
		{
			PreviewText = previewNode is null
				? "\u041d\u0435\u0442 \u0443\u0437\u043b\u0430"
				: $"\u0412\u044b\u0445\u043e\u0434\u044b: {previewNode.Outputs.Count}";
			PreviewBadge = previewNode?.PreviewMode ?? "2D";
		}
		else
		{
			foreach (var parameter in SelectedNode.Model.Parameters.ParameterDefinitions)
				InspectorParameters.Add(new InspectorParameterViewModel(
					parameter.Key,
					GraphNodeViewModel.Label(parameter.CustomLocKey ?? parameter.Key),
					SelectedNode.Model.Get(parameter.Key),
					UpdateSelectedParameter));

			PreviewText = $"\u0412\u044b\u0445\u043e\u0434\u044b: {SelectedNode.Outputs.Count}";
			PreviewBadge = SelectedNode.PreviewMode;
		}

		OnPropertyChanged(nameof(InspectorTitle));
		OnPropertyChanged(nameof(InspectorSubtitle));
		OnPropertyChanged(nameof(InspectorPreviewBadge));
		OnPropertyChanged(nameof(SelectedNodeStatus));
		OnPropertyChanged(nameof(ActivePreviewNode));
	}

	private void UpdateSelectedParameter(string key, float value)
	{
		if (SelectedNode is null)
			return;

		SelectedNode.Model.Parameters.Set(key, value);
		_document.NotifyNodeChanged(SelectedNode.Id);
		SelectedNode.RefreshPreview();
		OnPropertyChanged(nameof(ActivePreviewNode));
	}

	private static IReadOnlyList<INodeExecutor> CreateExecutors()
	{
		return
		[
			.. CreateExecutors(
				typeof(BaseNode).Assembly,
				typeof(SamplePointsNode).Assembly,
				typeof(DisplayWaterNode).Assembly)
		];
	}

	private static IEnumerable<INodeExecutor> CreateExecutors(params Assembly[] assemblies)
	{
		return assemblies
			.Distinct()
			.SelectMany(static assembly => assembly.GetTypes())
			.Where(static type => !type.IsAbstract && typeof(INodeExecutor).IsAssignableFrom(type))
			.Where(static type => type.GetConstructor(Type.EmptyTypes) is not null)
			.Select(static type => (INodeExecutor)Activator.CreateInstance(type)!);
	}

	private ObservableCollection<LibraryItemViewModel> BuildLibrary(IEnumerable<CatalogNodeViewModel> catalog)
	{
		var nodesRoot = new LibraryItemViewModel("\u0423\u0437\u043b\u044b", LibraryItemKind.Group);
		foreach (var group in catalog.GroupBy(static item => item.Group).OrderBy(static group => group.Key))
		{
			var groupItem = new LibraryItemViewModel(group.Key, LibraryItemKind.Group);
			foreach (var subgroup in group.GroupBy(static item => item.Subgroup).OrderBy(static subgroup => subgroup.Key))
			{
				var subgroupItem = new LibraryItemViewModel(subgroup.Key, LibraryItemKind.Group);
				foreach (var item in subgroup.OrderBy(static item => item.Name))
					subgroupItem.Children.Add(new LibraryItemViewModel(item.Name, LibraryItemKind.Node, item, ColorUtils.BrushColor(item.ColorBrush)));

				groupItem.Children.Add(subgroupItem);
			}

			nodesRoot.Children.Add(groupItem);
		}

		return
		[
			nodesRoot,
			new LibraryItemViewModel("Сессии", LibraryItemKind.Group),
			new LibraryItemViewModel("Макросы", LibraryItemKind.Group)
		];
	}

	private void RefreshSessionLibrary()
	{
		var sessions = Library.FirstOrDefault(static item => item.Name == "\u0421\u0435\u0441\u0441\u0438\u0438");
		if (sessions is null)
			return;

		sessions.Children.Clear();
		if (_savedSession is not null)
			sessions.Children.Add(new LibraryItemViewModel("Текущая", LibraryItemKind.Session));
	}

	private CatalogNodeViewModel? FindCatalogItem(string executorKey)
	{
		return Catalog.FirstOrDefault(item => item.ExecutorKey == executorKey);
	}

	private static CatalogNodeViewModel CreateCatalogItem(INodeExecutor executor)
	{
		return new CatalogNodeViewModel(
			executor.Key,
			GraphNodeViewModel.Label(executor.LocalizationKey),
			GraphNodeViewModel.Label(executor.GroupKey),
			GraphNodeViewModel.Label(executor.SubgroupKey),
			ColorUtils.ToAvaloniaColor(executor.Color));
	}


}
