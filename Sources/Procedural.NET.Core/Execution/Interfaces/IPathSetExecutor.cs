using NovoDwarf.Primitives.Models;

namespace Procedural.NET.Core.Execution.Interfaces;

public interface IPathSetExecutor
{
	PathSet EvaluatePaths(GraphNode model, IGraphExecutionContext graph, int width, int height);
}

public interface IMultiPathSetExecutor
{
	PathSet EvaluatePaths(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height);
}
