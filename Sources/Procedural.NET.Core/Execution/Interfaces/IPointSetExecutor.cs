using NovoDwarf.Primitives.Models;

namespace Procedural.NET.Core.Execution.Interfaces;

public interface IPointSetExecutor
{
	PointSet EvaluatePoints(GraphNode model, IGraphExecutionContext graph, int width, int height);
}

public interface IMultiPointSetExecutor
{
	PointSet EvaluatePoints(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height);
}
