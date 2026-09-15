namespace Procedural.NET.Storage;

public sealed class SessionNode
{
	public Guid Id { get; set; }
	public string ExecutorKey { get; set; } = string.Empty;
	public string? CustomName { get; set; }
	public float PositionX { get; set; }
	public float PositionY { get; set; }
	public float SizeX { get; set; }
	public float SizeY { get; set; }
	public Dictionary<string, float> Parameters { get; set; } = new(StringComparer.Ordinal);
	public Dictionary<string, string> StringParameters { get; set; } = new(StringComparer.Ordinal);
	public List<string> PinnedParameters { get; set; } = [];
}
