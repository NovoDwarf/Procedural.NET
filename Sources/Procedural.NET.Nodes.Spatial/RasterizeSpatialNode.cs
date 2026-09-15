
using NovoDwarf.Primitives.Models;
using NovoDwarf.Primitives.Models.Float2;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Spatial;

public sealed class RasterizeSpatialNode : BaseNode, IFieldExecutor
{
	public override string Key => NodeKeys.RasterizeSpatial;
	public override string GroupKey => NodeGroupKeys.Utilities;
	
	public override ColorF Color => new(0.34f, 0.48f, 0.34f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.Source, ValueShape.Any)
	];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.Mask, ValueShape.Field, PortSemantics.Mask)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Radius, 0f, 32f, 0.5f, 2f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Strength, 0f, 1f, 0.01f, 1f, Category: ParameterCategory.Advanced)
	];

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		if (!graph.TryValueInput(model, PortKeys.Source, width, height, out var source) || source is null)
			return new Field(width, height);

		return source switch
		{
			GraphValue.Raster raster => raster.Value,
			GraphValue.Scalar scalar => Fill(width, height, scalar.Value),
			GraphValue.Color color => Fill(width, height, (color.Value.R + color.Value.G + color.Value.B) / 3f),
			GraphValue.Bitmap bitmap => ToField(bitmap.Value),
			GraphValue.Points points => RasterizePoints(points.Value, width, height, graph.GetParameter(model, ParameterKeys.Radius, 0.5f, 0.5f), graph.GetParameter(model, ParameterKeys.Strength, 0.5f, 0.5f)),
			GraphValue.Paths paths => RasterizePaths(paths.Value, width, height, graph.GetParameter(model, ParameterKeys.Radius, 0.5f, 0.5f), graph.GetParameter(model, ParameterKeys.Strength, 0.5f, 0.5f)),
			_ => new Field(width, height)
		};
	}

	private static Field Fill(int width, int height, float value)
	{
		var field = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			field[x, y] = value;

		return field;
	}

	private static Field RasterizePoints(PointSet points, int width, int height, float radius, float strength)
	{
		var field = new Field(width, height);
		var baseRadius = Math.Max(0f, radius);
		var gain = Math.Max(0f, strength);

		foreach (var point in points.Points)
		{
			var pointRadius = Math.Max(0.5f, baseRadius > 0f ? baseRadius : point.Radius);
			var minX = Math.Max(0, (int)MathF.Floor(point.Position.X - pointRadius));
			var maxX = Math.Min(width - 1, (int)MathF.Ceiling(point.Position.X + pointRadius));
			var minY = Math.Max(0, (int)MathF.Floor(point.Position.Y - pointRadius));
			var maxY = Math.Min(height - 1, (int)MathF.Ceiling(point.Position.Y + pointRadius));

			for (var y = minY; y <= maxY; y++)
			for (var x = minX; x <= maxX; x++)
			{
				var distance = Float2.Distance(new Float2(x + 0.5f, y + 0.5f), point.Position);
				
				if (distance > pointRadius)
					continue;

				var value = (1f - distance / pointRadius) * point.Weight * gain;
				field[x, y] = Math.Max(field[x, y], value);
			}
		}

		return field;
	}

	private static Field RasterizePaths(PathSet paths, int width, int height, float radius, float strength)
	{
		var field = new Field(width, height);
		var gain = Math.Max(0f, strength);

		foreach (var path in paths.Paths)
		{
			if (path.Points.Length < 2)
				continue;

			var pathRadius = (float)Math.Max(0.5f, radius > 0f ? radius : path.Width);

			for (var i = 0; i < path.Points.Length - 1; i++)
			{
				var a = path.Points[i];
				var b = path.Points[i + 1];
				
				var minX = Math.Max(0, (int)MathF.Floor(Math.Min(a.X, b.X) - pathRadius));
				var maxX = Math.Min(width - 1, (int)MathF.Ceiling(Math.Max(a.X, b.X) + pathRadius));
				var minY = Math.Max(0, (int)MathF.Floor(Math.Min(a.Y, b.Y) - pathRadius));
				var maxY = Math.Min(height - 1, (int)MathF.Ceiling(Math.Max(a.Y, b.Y) + pathRadius));

				for (var y = minY; y <= maxY; y++)
				for (var x = minX; x <= maxX; x++)
				{
					var sample = new Float2(x + 0.5f, y + 0.5f);
					var distance = DistanceToSegment(sample, a, b);
					
					if (distance > pathRadius)
						continue;

					var value = (1f - distance / pathRadius) * path.Weight * gain;
					field[x, y] = Math.Max(field[x, y], value);
				}
			}
		}

		return field;
	}

	private static float DistanceToSegment(Float2 point, Float2 a, Float2 b)
	{
		var ab = b - a;
		var ap = point - a;
		var lengthSquared = ab.X * ab.X + ab.Y * ab.Y;
		
		if (lengthSquared <= 0.0001f)
			return Float2.Distance(point, a);

		var t = Math.Clamp((ap.X * ab.X + ap.Y * ab.Y) / lengthSquared, 0f, 1f);
		var closest = a + ab * t;
		
		return Float2.Distance(point, closest);
	}

	private static Field ToField(RgbaBitmap bitmap)
	{
		var field = new Field(bitmap.Width, bitmap.Height);
		for (var y = 0; y < bitmap.Height; y++)
		for (var x = 0; x < bitmap.Width; x++)
			field[x, y] = (bitmap.R[x, y] + bitmap.G[x, y] + bitmap.B[x, y]) / 3f;

		return field;
	}
}
