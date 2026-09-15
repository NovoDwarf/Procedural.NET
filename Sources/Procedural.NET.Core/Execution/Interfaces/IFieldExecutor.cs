using NovoDwarf.Primitives.Models;

namespace Procedural.NET.Core.Execution.Interfaces;

/// <summary>Single-output field executor. Implement when the graphNode has one field output.</summary>
public interface IFieldExecutor
{
	Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height);
}

/// <summary>Multi-output field executor. Implement when the graphNode has several field outputs distinguished by <paramref name="outputKey"/>.</summary>
public interface IMultiFieldExecutor
{
	Field EvaluateField(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height);
}
