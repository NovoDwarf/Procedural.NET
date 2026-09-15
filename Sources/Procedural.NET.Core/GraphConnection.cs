namespace Procedural.NET.Core;

public record struct GraphPort(Guid NodeId, string PortKey);
	
internal sealed class GraphConnection
{
	private readonly Dictionary<GraphPort, GraphPort> _forward = [];
	
	private readonly Dictionary<Guid, HashSet<Guid>> _upstream = [];
	private readonly Dictionary<Guid, HashSet<Guid>> _downstream = [];

	public bool TryGetOutput(Guid inputNodeId, string inputPortKey, out GraphPort output) =>
		_forward.TryGetValue(new GraphPort(inputNodeId, inputPortKey), out output);

	public bool Contains(Guid inputNodeId, string inputPortKey) 
		=> _forward.ContainsKey(new GraphPort(inputNodeId, inputPortKey));

	public IEnumerable<(GraphPort Input, GraphPort Output)> GetAll()
		=> _forward.Select(kvp => (kvp.Key, kvp.Value));

	public void Connect(GraphPort output, GraphPort input)
	{
		if (_forward.TryGetValue(input, out var prev))
			RemoveLink(prev.NodeId, input.NodeId);

		_forward[input] = output;

		AddLink(output.NodeId, input.NodeId);
	}
	
	public HashSet<Guid> GetAncestorIds(Guid nodeId) 
		=> TraverseUpstream(nodeId);
	
	public HashSet<Guid> GetAffectedIds(Guid startId)
		=> TraverseDownstream(startId, true);

	public HashSet<Guid> GetDescendantIds(Guid nodeId)
		=> TraverseDownstream(nodeId, false);
	
	public bool TryRemoveVerified(GraphPort input, GraphPort expectedOutput)
	{
		if (!_forward.TryGetValue(input, out var actual) || actual != expectedOutput)
			return false;

		Disconnect(input, expectedOutput);
		return true;
	}

	public void RemoveAllForNodes(IReadOnlySet<Guid> nodeIds)
	{
		RemoveWhere(p =>
			nodeIds.Contains(p.Key.NodeId) ||
			nodeIds.Contains(p.Value.NodeId));

		foreach (var id in nodeIds)
		{
			_downstream.Remove(id);
			_upstream.Remove(id);
		}
	}

	public bool RemoveInvalidFor(Guid nodeId, IReadOnlySet<string> validInputs, IReadOnlySet<string> validOutputs)
	{
		return RemoveWhere(p =>
			(p.Key.NodeId == nodeId && !validInputs.Contains(p.Key.PortKey)) ||
			(p.Value.NodeId == nodeId && !validOutputs.Contains(p.Value.PortKey))) > 0;
	}

	public void Clear()
	{
		_forward.Clear();
		_downstream.Clear();
		_upstream.Clear();
	}

	public GraphConnection Copy()
	{
		var copy = new GraphConnection();

		foreach (var (input, output) in _forward)
		{
			copy._forward[input] = output;
			copy.AddLink(output.NodeId, input.NodeId);
		}

		return copy;
	}
	
	private void Disconnect(GraphPort input, GraphPort output)
	{
		_forward.Remove(input);
		RemoveLink(output.NodeId, input.NodeId);
	}

	private int RemoveWhere(Func<KeyValuePair<GraphPort, GraphPort>, bool> predicate)
	{
		var toRemove = new List<(GraphPort Input, GraphPort Output)>();

		foreach (var pair in _forward)
		{
			if (predicate(pair))
				toRemove.Add((pair.Key, pair.Value));
		}

		foreach (var (input, output) in toRemove)
			Disconnect(input, output);

		return toRemove.Count;
	}
	
	private void AddLink(Guid outputNodeId, Guid inputNodeId)
	{
		if (!_downstream.TryGetValue(outputNodeId, out var set))
			_downstream[outputNodeId] = set = [];

		set.Add(inputNodeId);

		if (!_upstream.TryGetValue(inputNodeId, out var parents))
			_upstream[inputNodeId] = parents = [];

		parents.Add(outputNodeId);
	}

	private void RemoveLink(Guid outputNodeId, Guid inputNodeId)
	{
		if (_downstream.TryGetValue(outputNodeId, out var set))
			set.Remove(inputNodeId);

		if (_upstream.TryGetValue(inputNodeId, out var parents))
			parents.Remove(outputNodeId);
	}

	private HashSet<Guid> TraverseUpstream(Guid start)
	{
		var visited = new HashSet<Guid>();
		var queue = new Queue<Guid>();

		queue.Enqueue(start);

		while (queue.Count > 0)
		{
			if (!_upstream.TryGetValue(queue.Dequeue(), out var parents))
				continue;

			foreach (var parent in parents)
			{
				if (visited.Add(parent))
					queue.Enqueue(parent);
			}
		}

		return visited;
	}
	
	private HashSet<Guid> TraverseDownstream(Guid start, bool includeRoot)
	{
		var visited = new HashSet<Guid>();

		if (includeRoot)
			visited.Add(start);

		var queue = new Queue<Guid>();
		queue.Enqueue(start);

		while (queue.Count > 0)
		{
			if (!_downstream.TryGetValue(queue.Dequeue(), out var set))
				continue;

			foreach (var id in set)
				if (visited.Add(id))
					queue.Enqueue(id);
		}

		return visited;
	}
}
