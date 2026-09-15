using Procedural.NET.Core.Execution;

namespace Procedural.NET.Designer.Compute;

public interface IComputeBackend
{
	string Name { get; }
	bool Supports(string executorKey);
	GraphValue Evaluate(ComputeRequest request);
}

public sealed record ComputeRequest(string ExecutorKey, GraphEvaluationRequest EvaluationRequest);
