using NovoDwarf.Primitives.Models.Float2;

namespace Procedural.NET.Core;

public sealed class GraphCommands
{
	private readonly GraphFactory _factory;

	public GraphCommands(GraphDocument document, GraphFactory factory)
	{
		Document = document;
		
		_factory = factory;
	}

	public GraphDocument Document { get; }

	public GraphNode Add(string executorKey, Float2 position)
	{
		var node = _factory.CreateNode(executorKey, position);
		
		Document.AddNode(node);
		
		return node;
	}

	public GraphNode Create(string executorKey, Float2 position)
	{
		return _factory.CreateNode(executorKey, position);
	}

	public GraphNode Duplicate(GraphNode source, Float2 position)
	{
		var node = _factory.CreateNode(source.Executor.Key, position);
		
		node.CopyFrom(source);
		source.Executor.UpdatePorts(node);
		
		Document.AddNode(node);
		
		return node;
	}
	
	public bool RefreshDynamicPorts(GraphNode graphNode)
	{
		if (!graphNode.Executor.UpdatePorts(graphNode))
			return false;

		Document.RemoveInvalidConnections(graphNode);
		Document.NotifyNodeChanged(graphNode.Id);

		return true;
	}
}
