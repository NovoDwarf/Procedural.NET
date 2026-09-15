namespace Procedural.NET.Core.Execution.Interfaces;

/// <summary>Single-output sampled executor. Returns a scalar at a UV point.</summary>
public interface ISampledExecutor
{
	float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context);
}

/// <summary>Multi-output sampled executor. Returns a scalar for the given <paramref name="outputKey"/> at a UV point.</summary>
public interface IMultiSampledExecutor
{
	float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, string outputKey, EvaluationContext context);
}
