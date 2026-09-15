using NovoDwarf.Primitives.Models;

namespace Procedural.NET.Core.Execution;

public abstract record GraphValue
{
	public sealed record Scalar(float Value) : GraphValue;
	public sealed record Color(RgbaColor Value) : GraphValue;
	public sealed record Raster(Field Value) : GraphValue;
	public sealed record Bitmap(RgbaBitmap Value) : GraphValue;
	public sealed record Points(PointSet Value) : GraphValue;
	public sealed record Paths(PathSet Value) : GraphValue;
}
