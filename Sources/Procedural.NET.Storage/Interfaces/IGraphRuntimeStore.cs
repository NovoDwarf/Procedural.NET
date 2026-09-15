namespace Procedural.NET.Storage.Interfaces;

public interface IGraphRuntimeStore
{
	IGraphSessionRepository Sessions { get; }
	IGraphNodePresetRepository Presets { get; }
	IGraphNodeCatalogRepository Nodes { get; }
}
