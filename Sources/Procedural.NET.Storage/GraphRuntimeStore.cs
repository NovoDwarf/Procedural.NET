using Procedural.NET.Storage.Interfaces;

namespace Procedural.NET.Storage;

public sealed class GraphRuntimeStore(
	IGraphSessionRepository sessions,
	IGraphNodePresetRepository presets,
	IGraphNodeCatalogRepository nodes) : IGraphRuntimeStore
{
	public IGraphSessionRepository Sessions { get; } = sessions;
	public IGraphNodePresetRepository Presets { get; } = presets;
	public IGraphNodeCatalogRepository Nodes { get; } = nodes;
}
