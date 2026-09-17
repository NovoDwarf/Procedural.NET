using Procedural.NET.Core;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Terrain.Execution.Interfaces;

/// <summary>
/// Implemented by the terminal export graphNode to produce a <see cref="WorldExportResult"/>
/// from a fully-connected graph.
/// </summary>
public interface IWorldExportExecutor
{
	WorldExportResult EvaluateExport(GraphNode model, IGraphExecutionContext ctx, int resolution);
}
