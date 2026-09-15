using NovoDwarf.Primitives.Models;


namespace Procedural.NET.Nodes.Terrain.Execution;

/// <summary>
/// Result produced by evaluating a <c>WorldExportNode</c>.
/// Elevation is always present; water and scatter layers are optional.
/// </summary>
public sealed class WorldExportResult
{
	public required Field Elevation { get; init; }
	public float SeaLevel { get; init; }
	public Field? OceanMask { get; init; }
	public PointSet? Points { get; init; }
	public PathSet? Paths { get; init; }
	public IReadOnlyList<WaterSlot> WaterLayers { get; init; } = [];
	public IReadOnlyList<ScatterSlot> ScatterLayers { get; init; } = [];
	public string TerrainPaintMapPath { get; init; } = string.Empty;
	public RgbaBitmap? TerrainPaintBitmap { get; init; }
	/// <summary>
	/// Topographic map baked from elevation.
	/// Uses terrain tinting, hillshade and contour lines.
	/// </summary>
	public RgbaBitmap? TopographicMap { get; init; }
	/// <summary>
	/// World-space normal map baked from the elevation field.
	/// Packed as (n + 1) * 0.5 → R=X, G=Y, B=Z.
	/// Always present when produced by <c>WorldExportNode</c>.
	/// </summary>
	public RgbaBitmap? NormalMap { get; init; }
	/// <summary>
	/// 4-channel splat map: R=layer0(grass), G=layer1(dirt), B=layer2(rock), A=layer3(snow).
	/// Procedurally generated from elevation + slope when no explicit input is provided.
	/// </summary>
	public RgbaBitmap? SplatMap { get; init; }
	/// <summary>Horizon-based AO baked from elevation. 0 = fully occluded, 1 = open sky.</summary>
	public Field? AmbientOcclusionMap { get; init; }
	/// <summary>
	/// Cavity map from elevation Laplacian.
	/// 0.5 = neutral; &lt;0.5 = concave (valleys); &gt;0.5 = convex (ridges).
	/// </summary>
	public Field? CavityMap { get; init; }
	/// <summary>
	/// Approximated flow/wetness: concave areas that collect water. 0 = dry, 1 = wet.
	/// </summary>
	public Field? FlowMap { get; init; }
}

/// <summary>One water layer: a normalised depth/presence Field (0 = dry, 1 = fully covered).</summary>
public sealed record WaterSlot(int Index, Field Mask);

/// <summary>One asset-scatter layer: a normalised density Field (0 = empty, 1 = max density).</summary>
public sealed record ScatterSlot(int Index, Field Density, string DefinitionPath = "");
