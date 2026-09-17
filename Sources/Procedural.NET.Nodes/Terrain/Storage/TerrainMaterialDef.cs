namespace Procedural.NET.Nodes.Terrain.Storage;

/// <summary>
/// One of the four splat layers that make up the terrain material.
/// Texture paths are Godot <c>res://</c> paths or absolute filesystem paths.
/// </summary>
public sealed class TerrainLayerDef
{
	/// <summary>Tiled albedo texture path. Empty = use procedural vertex colour.</summary>
	public string AlbedoPath { get; set; } = string.Empty;
	/// <summary>Tiled normal-map texture path. Empty = flat normals.</summary>
	public string NormalPath { get; set; } = string.Empty;
	/// <summary>How many times the tiled textures repeat across the full terrain mesh.</summary>
	public float TextureScale { get; set; } = 20f;
	/// <summary>Perceptual roughness for this layer (0 = mirror, 1 = fully matte).</summary>
	public float Roughness { get; set; } = 0.85f;
}

/// <summary>
/// Four-layer splat material definition loaded from a JSON asset.
/// Layer order matches the splat-map RGBA channels: R=layer0, G=layer1, B=layer2, A=layer3.
/// </summary>
public sealed class TerrainMaterialDef
{
	/// <summary>Grass / low-altitude flat ground.</summary>
	public TerrainLayerDef Layer0 { get; set; } = new();
	/// <summary>Dirt / mid-altitude transitional ground.</summary>
	public TerrainLayerDef Layer1 { get; set; } = new();
	/// <summary>Rock / steep-slope surface.</summary>
	public TerrainLayerDef Layer2 { get; set; } = new();
	/// <summary>Snow / high-altitude peak.</summary>
	public TerrainLayerDef Layer3 { get; set; } = new();

	public TerrainLayerDef this[int index] => index switch
	{
		0 => Layer0,
		1 => Layer1,
		2 => Layer2,
		3 => Layer3,
		_ => throw new ArgumentOutOfRangeException(nameof(index))
	};
}

/// <summary>Loads and caches <see cref="TerrainMaterialDef"/> assets by path.</summary>
public interface ITerrainMaterialLibrary
{
	TerrainMaterialDef? TryLoad(string path);
}
