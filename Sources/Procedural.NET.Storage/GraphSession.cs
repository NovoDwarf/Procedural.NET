using Procedural.NET.Core;

namespace Procedural.NET.Storage;

public sealed class GraphSession
{
	public const int CurrentVersion = 1;

	public int FormatVersion { get; set; } = CurrentVersion;
	public GraphMetadata Metadata { get; set; } = new();

	public List<SessionNode> Nodes { get; set; } = [];
	public List<SessionConnection> Connections { get; set; } = [];
}
