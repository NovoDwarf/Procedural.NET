using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Storage.Interfaces;

public interface IGraphNodeCatalogRepository
{
	IReadOnlyList<INodeExecutor> GetExecutors();
}
