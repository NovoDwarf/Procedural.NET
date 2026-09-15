namespace Procedural.NET.Core;

public sealed class GraphMetadata
{
	public string Name { get; set; } = "Untitled";
	public string Description { get; set; } = string.Empty;
	public string Author { get; set; } = string.Empty;
	public string CreatedUtc { get; set; } = DateTime.UtcNow.ToString("O");
	public string ModifiedUtc { get; set; } = DateTime.UtcNow.ToString("O");
	public int Seed { get; set; }
	public string GeneratorVersion { get; set; } = "1.0.0";
	public int AreaWidth { get; set; } = 4096;
	public int AreaHeight { get; set; } = 4096;
	public float OriginX { get; set; }
	public float OriginY { get; set; }
	public float AspectRatio { get; set; } = 1f;
	public int BuildResolutionPower { get; set; } = 9;
	public string BuildResolutionMode { get; set; } = "power_of_two_plus_one";
	public float SeaLevel { get; set; }
	public string ColorPreset { get; set; } = "terrain_default";
	public string Units { get; set; } = "meters";
	public string RescalingMode { get; set; } = "bicubic";
	public Dictionary<string, string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
