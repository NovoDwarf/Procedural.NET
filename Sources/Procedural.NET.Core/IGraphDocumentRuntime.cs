namespace Procedural.NET.Core;

public interface IGraphDocumentRuntime
{
	bool HasInputConnection(Guid nodeId, string portKey);
	bool TryGetInputConnection(Guid inputNodeId, string inputPortKey, out GraphPort output);
}
