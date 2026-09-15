namespace Procedural.NET.Core.Commands;

public sealed class RemoveNodesCommand : IGraphCommand
{
	private readonly GraphDocument _document;
	private readonly GraphNode[] _nodes;
	private readonly ConnectionSnapshot[] _connections;

	public string Name { get; }

	public RemoveNodesCommand(string name, GraphDocument document, IReadOnlyList<GraphNode> nodes)
	{
		Name = name;
		
		_document = document;
		_nodes = [..nodes];

		var removedIds = nodes.Select(static n => n.Id).ToHashSet();
		
		_connections =
		[
			.. document.GetAllConnections()
			           .Where(c => removedIds.Contains(c.Input.NodeId) || removedIds.Contains(c.Output.NodeId))
			           .Select(static c =>
				           new ConnectionSnapshot(c.Input.NodeId, c.Input.PortKey, c.Output.NodeId, c.Output.PortKey))
		];
	}

	public IReadOnlyList<GraphNode> Nodes => _nodes;

	public void Execute() => _document.RemoveNodes(_nodes);

	public void Undo()
	{
		foreach (var node in _nodes)
			_document.AddNode(node);

		foreach (var conn in _connections)
			_document.TryConnect(conn.OutputNodeId, conn.OutputPortKey, conn.InputNodeId, conn.InputPortKey);
	}

	private readonly record struct ConnectionSnapshot(Guid InputNodeId, string InputPortKey, Guid OutputNodeId, string OutputPortKey);
}
