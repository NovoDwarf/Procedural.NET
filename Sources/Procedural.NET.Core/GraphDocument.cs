using Procedural.NET.Core.Caching;
using Procedural.NET.Core.Execution;

namespace Procedural.NET.Core;

public sealed class GraphDocument : IGraphRuntimeDocument, IGraphDocumentRuntime
{
	private readonly List<GraphNode> _nodes = [];
	
	private readonly Dictionary<Guid, GraphNode> _nodesById = [];
	private readonly GraphConnection _graphConnections = new();
	private readonly GraphSelection _graphSelection = new();
	private readonly GraphCache _graphCache = new();
	
	public long GraphVersion { get; private set; }
	public GraphMetadata Metadata { get; set; } = new();

	public GraphNode? SelectedNode => _graphSelection.SelectedNode;
	public IReadOnlyList<GraphNode> SelectedNodes => _graphSelection.SelectedNodes;
	public IReadOnlyList<GraphNode> Nodes => _nodes;
	public IReadOnlySet<Guid>? LastChangedAffectedIds { get; private set; }

	public event Action? Changed;
	public event Action<GraphNode?>? SelectionChanged
	{
		add => _graphSelection.SelectionChanged += value;
		remove => _graphSelection.SelectionChanged -= value;
	}
	
	public GraphNode GetNode(Guid id) => _nodesById[id];

	public void AddNode(GraphNode graphNode)
	{
		_nodes.Add(graphNode);
		_nodesById[graphNode.Id] = graphNode;
		_graphSelection.Set(graphNode);
		
		NotifyChanged([graphNode.Id]);
	}

	public void RemoveNodes(IEnumerable<GraphNode> nodes)
	{
		var removedIds = nodes.Select(static n => n.Id).ToHashSet();
		
		if (removedIds.Count == 0)
			return;

		_nodes.RemoveAll(n => removedIds.Contains(n.Id));
		_graphSelection.RemoveAll(removedIds);
		
		foreach (var id in removedIds) 
			_nodesById.Remove(id);
		
		_graphConnections.RemoveAllForNodes(removedIds);
		_graphCache.Clear();
		
		NotifyChanged(null);
	}

	public void NotifyChanged(HashSet<Guid>? changed)
	{
		LastChangedAffectedIds = changed;
		PublishChanged();
	}
	
	public void NotifyNodeChanged(Guid nodeId)
	{
		var affected = _graphConnections.GetAffectedIds(nodeId);
		
		_graphCache.Invalidate(affected);
		
		LastChangedAffectedIds = affected;
		PublishChanged();
	}

	public bool TryConnect(GraphNode outputGraphNode, GraphNodePort outputPort, GraphNode inputGraphNode, GraphNodePort inputPort)
	{
		if (outputGraphNode.Id == inputGraphNode.Id)
			return false;

		if (!GraphRules.ArePortsCompatible(outputPort, inputPort))
			return false;

		_graphConnections.Connect(new GraphPort(outputGraphNode.Id, outputPort.Key), new GraphPort(inputGraphNode.Id,  inputPort.Key));

		var affected = _graphConnections.GetAffectedIds(inputGraphNode.Id);
		_graphCache.Invalidate(affected);
		
		NotifyChanged(affected);
		
		return true;
	}

	public bool TryConnect(Guid outputNodeId, string outputPortKey, Guid inputNodeId, string inputPortKey)
	{
		if (!_nodesById.TryGetValue(outputNodeId, out var outputNode) || !_nodesById.TryGetValue(inputNodeId,  out var inputNode))
			return false;

		var outputPort = outputNode.Outputs.FirstOrDefault(p => p.Key == outputPortKey);
		var inputPort = inputNode.Inputs
			.Concat(inputNode.Parameters.ParameterInputPortsByParameter.Values)
			.FirstOrDefault(p => p.Key == inputPortKey);

		return TryConnect(outputNode, outputPort, inputNode, inputPort);
	}

	public void RemoveConnection(Guid inputNodeId, string inputPortKey, Guid outputNodeId, string outputPortKey)
	{
		if (!_graphConnections.TryRemoveVerified(
			    new GraphPort(inputNodeId, inputPortKey),
			    new GraphPort(outputNodeId, outputPortKey)))
			return;

		var affected = _graphConnections.GetAffectedIds(inputNodeId);
		_graphCache.Invalidate(affected);

		NotifyChanged(affected);
	}

	public void RemoveInvalidConnections(GraphNode graphNode)
	{
		var validInputs = graphNode.Inputs.Select(static p => p.Key)
			.Concat(graphNode.Parameters.ParameterInputPortsByParameter.Values.Select(static p => p.Key))
			.ToHashSet(StringComparer.Ordinal);

		var validOutputs = graphNode.Outputs.Select(static p => p.Key).ToHashSet(StringComparer.Ordinal);

		if (!_graphConnections.RemoveInvalidFor(graphNode.Id, validInputs, validOutputs))
			return;

		var affected = _graphConnections.GetAffectedIds(graphNode.Id);
		_graphCache.Invalidate(affected);
		
		NotifyChanged(affected);

	}

	public bool HasInputConnection(Guid nodeId, string portKey) 
		=> _graphConnections.Contains(nodeId, portKey);
	
	public IEnumerable<(GraphPort Input, GraphPort Output)> GetAllConnections()
		=> _graphConnections.GetAll();

	public HashSet<Guid> GetAncestorIds(Guid nodeId) 
		=> _graphConnections.GetAncestorIds(nodeId);
	
	public HashSet<Guid> GetDescendantIds(Guid nodeId) 
		=> _graphConnections.GetDescendantIds(nodeId);
	
	public bool TryGetInputConnection(Guid inputNodeId, string inputPortKey, out GraphPort output)
		=> _graphConnections.TryGetOutput(inputNodeId, inputPortKey, out output);

	public void SetSelection(IEnumerable<GraphNode> nodes) => _graphSelection.Set(nodes);

	public void ClearSelection() => _graphSelection.Clear();

	public bool TryGetCachedField(Guid outputNodeId, string outputPortKey, int width, int height, out NovoDwarf.Primitives.Models.Field buffer)
		=> _graphCache.TryGetField(outputNodeId, outputPortKey, width, height, out buffer);

	public void SetCachedField(Guid outputNodeId, string outputPortKey, int width, int height, NovoDwarf.Primitives.Models.Field buffer)
		=> _graphCache.SetField(outputNodeId, outputPortKey, width, height, buffer);

	public bool TryGetCachedPointSet(Guid outputNodeId, string outputPortKey, int width, int height, out NovoDwarf.Primitives.Models.PointSet buffer)
		=> _graphCache.TryGetPointSet(outputNodeId, outputPortKey, width, height, out buffer);

	public void SetCachedPointSet(Guid outputNodeId, string outputPortKey, int width, int height, NovoDwarf.Primitives.Models.PointSet buffer)
		=> _graphCache.SetPointSet(outputNodeId, outputPortKey, width, height, buffer);

	public bool TryGetCachedPathSet(Guid outputNodeId, string outputPortKey, int width, int height, out NovoDwarf.Primitives.Models.PathSet buffer)
		=> _graphCache.TryGetPathSet(outputNodeId, outputPortKey, width, height, out buffer);

	public void SetCachedPathSet(Guid outputNodeId, string outputPortKey, int width, int height, NovoDwarf.Primitives.Models.PathSet buffer)
		=> _graphCache.SetPathSet(outputNodeId, outputPortKey, width, height, buffer);

	public void Clear()
	{
		_nodes.Clear();
		_nodesById.Clear();
		_graphConnections.Clear();
		_graphSelection.Clear();
		_graphCache.Clear();
		
		NotifyChanged(null);
	}

	internal GraphConnection CaptureConnections() => _graphConnections.Copy();
	
	private void PublishChanged()
	{
		GraphVersion++;
		Changed?.Invoke();
	}
}
