using NovoDwarf.Primitives.Models.Float2;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Core;

public class GraphFactory
{
	private readonly Dictionary<string, INodeExecutor> _executors;

	public GraphFactory(IEnumerable<INodeExecutor> executors)
	{
		_executors = executors.ToDictionary(static e => e.Key, StringComparer.Ordinal);
	}

	public GraphNode CreateNode(string executorKey, Float2 position)
		=> CreateNode(executorKey, Guid.NewGuid(), position);

	public GraphNode CreateNode(string executorKey, Guid id, Float2 position)
	{
		if (!_executors.TryGetValue(executorKey, out var executor))
			throw new KeyNotFoundException($"Node executor [{executorKey}] is not registered.");

		return new GraphNode(id, executor, position);
	}
}