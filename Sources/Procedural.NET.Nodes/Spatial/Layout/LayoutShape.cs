namespace Procedural.NET.Nodes.Spatial.Layout;

public sealed record LayoutShape
{
	public LayoutShapeKind Kind { get; init; }
	public float X { get; init; } = 0.5f;
	public float Y { get; init; } = 0.5f;
	public float Width { get; init; } = 0.35f;
	public float Height { get; init; } = 0.35f;
	public float Strength { get; init; } = 1f;
	public List<LayoutPoint> Points { get; init; } = [];
}
