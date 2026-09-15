using NovoDwarf.Primitives.Models;


namespace Procedural.NET.Core.Execution.Interfaces;

/// <summary>Single-output color executor.</summary>
public interface IColorExecutor
{
	RgbaColor EvaluateColor(GraphNode model, IGraphExecutionContext graph, EvaluationContext context);
}

/// <summary>Multi-output color executor.</summary>
public interface IMultiColorExecutor
{
	RgbaColor EvaluateColor(GraphNode model, IGraphExecutionContext graph, string outputKey, EvaluationContext context);
}
