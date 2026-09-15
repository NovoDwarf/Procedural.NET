namespace Procedural.NET.Storage;

public sealed class SessionConnection
{
	public Guid InputNodeId { get; set; }
	public string InputPortKey { get; set; } = string.Empty;
	public Guid OutputNodeId { get; set; }
	public string OutputPortKey { get; set; } = string.Empty;
}
