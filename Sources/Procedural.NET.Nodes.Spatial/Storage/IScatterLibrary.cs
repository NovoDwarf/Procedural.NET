namespace Procedural.NET.Nodes.Spatial.Storage;

public interface IScatterLibrary
{
	ScatterDefinition? TryLoad(string path);
}

public sealed class ScatterDefinition
{
	public string ScenePath { get; set; } = string.Empty;
	/// <summary>Minimum world-space distance between instances.</summary>
	public float Spacing { get; set; } = 1f;
	public float ScaleMin { get; set; } = 1f;
	public float ScaleMax { get; set; } = 1f;
	/// <summary>
	/// When true, instances are batched into a single <c>MultiMeshInstance3D</c>.
	/// Requires the scene to contain a <c>MeshInstance3D</c> at its root or as a direct child.
	/// Falls back to individual graphNode placement if the mesh cannot be extracted.
	/// </summary>
	public bool UseGpuInstancing { get; set; } = true;
}
