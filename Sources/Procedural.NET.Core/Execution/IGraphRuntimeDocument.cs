using NovoDwarf.Primitives.Models;

namespace Procedural.NET.Core.Execution;

public interface IGraphRuntimeDocument
{
	GraphMetadata Metadata { get; }
	GraphNode GetNode(Guid id);
	bool TryGetInputConnection(Guid inputNodeId, string inputPortKey, out GraphPort output);
	bool TryGetCachedField(Guid outputNodeId, string outputPortKey, int width, int height, out Field buffer);
	void SetCachedField(Guid outputNodeId, string outputPortKey, int width, int height, Field buffer);
	bool TryGetCachedPointSet(Guid outputNodeId, string outputPortKey, int width, int height, out PointSet buffer);
	void SetCachedPointSet(Guid outputNodeId, string outputPortKey, int width, int height, PointSet buffer);
	bool TryGetCachedPathSet(Guid outputNodeId, string outputPortKey, int width, int height, out PathSet buffer);
	void SetCachedPathSet(Guid outputNodeId, string outputPortKey, int width, int height, PathSet buffer);
}