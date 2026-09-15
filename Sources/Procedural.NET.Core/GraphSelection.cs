namespace Procedural.NET.Core;

public class GraphSelection
{
	private readonly List<GraphNode> _selectedNodes = [];
	private readonly HashSet<Guid> _selectedNodeIds = [];
	
	public GraphNode? SelectedNode { get; private set; }
	public IReadOnlyList<GraphNode> SelectedNodes => _selectedNodes;
	
	public event Action<GraphNode?>? SelectionChanged;

	public void Set(GraphNode? node)
	{
		if (SelectedNode == node && _selectedNodeIds.Count == (node is null ? 0 : 1))
			return;

		_selectedNodeIds.Clear();
		_selectedNodes.Clear();
		
		SelectedNode = node;

		if (node != null)
		{
			_selectedNodeIds.Add(node.Id);
			_selectedNodes.Add(node);
		}

		SelectionChanged?.Invoke(node);
	}
	
	public void Set(IEnumerable<GraphNode> nodes)
	{
		var selection = nodes.ToList();
		var newIds = selection.Select(static n => n.Id).ToHashSet();
		var newSelectedNode = selection.LastOrDefault();

		if (_selectedNodeIds.SetEquals(newIds) && SelectedNode?.Id == newSelectedNode?.Id)
			return;

		_selectedNodeIds.Clear();
		_selectedNodes.Clear();

		foreach (var node in selection)
		{
			_selectedNodeIds.Add(node.Id);
			_selectedNodes.Add(node);
		}

		SelectedNode = newSelectedNode;
		SelectionChanged?.Invoke(SelectedNode);
	}
	
	public void RemoveAll(HashSet<Guid> removedIds) 
	{
		foreach (var id in removedIds)
		{
			_selectedNodeIds.Remove(id);
		}
		
		_selectedNodes.RemoveAll(n => removedIds.Contains(n.Id));
		
		if (SelectedNode != null && removedIds.Contains(SelectedNode.Id))
			SelectedNode = _selectedNodes.LastOrDefault();
		
		SelectionChanged?.Invoke(SelectedNode);
	}
	
	public void Clear() => Set((GraphNode?)null);
	
}
