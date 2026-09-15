using Procedural.NET.Core.Execution;

namespace Procedural.NET.Designer.Compute;

public sealed class CpuComputeBackend : IComputeBackend
{
	public string Name => "CPU";

	public bool Supports(string executorKey) => true;

	public GraphValue Evaluate(ComputeRequest request)
	{
		return request.EvaluationRequest.Node.Executor.Evaluate(request.EvaluationRequest.Node, new NullGraphExecutionContext(), request.EvaluationRequest);
	}
}
