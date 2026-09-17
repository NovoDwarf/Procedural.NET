namespace Procedural.NET.Nodes.Terrain.Execution;

public interface IWorldGraphEvaluator
{
	Task<WorldExportResult> EvaluateAsync(
		string sessionPath,
		int resolution,
		CancellationToken ct = default);
}
