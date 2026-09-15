using Procedural.NET.Core.Execution;

namespace Procedural.NET.Designer.Compute;

public sealed class SlangComputeBackend : IComputeBackend
{
	public string Name => "GPU";

	public bool Supports(string executorKey) => false;

	public GraphValue Evaluate(ComputeRequest request)
	{
		throw new NotSupportedException("Slang backend is reserved for GPU node kernels and is not enabled in the MVP.");
	}
}
