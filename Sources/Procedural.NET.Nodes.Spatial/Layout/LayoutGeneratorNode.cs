
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Spatial.Layout;

public sealed class LayoutGeneratorNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	public override string Key => NodeKeys.LayoutGenerator;
	public override string GroupKey => NodeGroupKeys.Generators;
	public override string SubgroupKey => NodeSubgroupKeys.Layout;
	public override ColorF Color => new(0.35f, 0.44f, 0.24f);

	public override IReadOnlyList<GraphNodePort> Inputs => [];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.C, ValueShape.Field, PortSemantics.Terrain),
		new(PortKeys.Mask, ValueShape.Field, PortSemantics.Mask)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Falloff, 0f, 0.5f, 0.001f, 0.05f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Strength, 0f, 1f, 0.01f, 1f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Invert, 0f, 1f, 1f, 0f,
			UseSlider: false, Kind: ParameterKind.Checkbox, Category: ParameterCategory.Primary)
	];

	public override bool AllowParameterConnections => false;

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var layout = LayoutDocument.Parse(model.GetString(ParameterKeys.LayoutShapes));
		var falloff = Math.Clamp(graph.GetParameter(model, ParameterKeys.Falloff, context), 0f, 0.5f);
		var strength = Math.Clamp(graph.GetParameter(model, ParameterKeys.Strength, context), 0f, 1f);
		var value = Evaluate(layout, context.U, context.V, falloff) * strength;

		return graph.GetParameter<bool>(model, ParameterKeys.Invert, context)
			? 1f - value
			: value;
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var layout = LayoutDocument.Parse(model.GetString(ParameterKeys.LayoutShapes));
		var falloff = Math.Clamp(graph.GetParameter(model, ParameterKeys.Falloff, 0.5f, 0.5f), 0f, 0.5f);
		var strength = Math.Clamp(graph.GetParameter(model, ParameterKeys.Strength, 0.5f, 0.5f), 0f, 1f);
		var invert = graph.GetParameter<bool>(model, ParameterKeys.Invert, 0.5f, 0.5f);
		var result = new Field(width, height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var u = width <= 1 ? 0.5f : x / (float)(width - 1);
			var v = height <= 1 ? 0.5f : y / (float)(height - 1);
			var value = Evaluate(layout, u, v, falloff) * strength;
			result[x, y] = invert ? 1f - value : value;
		}

		return result;
	}

	private static float Evaluate(LayoutDocument layout, float u, float v, float falloff)
	{
		var value = 0f;
		foreach (var shape in layout.Shapes)
			value = MathF.Max(value, EvaluateShape(shape, u, v, falloff) * Math.Clamp(shape.Strength, 0f, 1f));

		return Math.Clamp(value, 0f, 1f);
	}

	private static float EvaluateShape(LayoutShape shape, float u, float v, float falloff)
	{
		return shape.Kind switch
		{
			LayoutShapeKind.Circle => EvaluateEllipse(shape, u, v, falloff),
			LayoutShapeKind.Polygon => EvaluatePolygon(shape, u, v, falloff),
			_ => EvaluateRectangle(shape, u, v, falloff)
		};
	}

	private static float EvaluateRectangle(LayoutShape shape, float u, float v, float falloff)
	{
		var halfW = MathF.Max(0.001f, shape.Width * 0.5f);
		var halfH = MathF.Max(0.001f, shape.Height * 0.5f);
		var dx = halfW - MathF.Abs(u - shape.X);
		var dy = halfH - MathF.Abs(v - shape.Y);
		return EdgeValue(MathF.Min(dx, dy), falloff);
	}

	private static float EvaluateEllipse(LayoutShape shape, float u, float v, float falloff)
	{
		var rx = MathF.Max(0.001f, shape.Width * 0.5f);
		var ry = MathF.Max(0.001f, shape.Height * 0.5f);
		var nx = (u - shape.X) / rx;
		var ny = (v - shape.Y) / ry;
		var distance = MathF.Sqrt(nx * nx + ny * ny);
		var signed = (1f - distance) * MathF.Min(rx, ry);
		return EdgeValue(signed, falloff);
	}

	private static float EvaluatePolygon(LayoutShape shape, float u, float v, float falloff)
	{
		if (shape.Points.Count < 3)
			return 0f;

		var inside = IsInsidePolygon(shape.Points, u, v);
		var distance = DistanceToPolygon(shape.Points, u, v);
		return EdgeValue(inside ? distance : -distance, falloff);
	}

	private static bool IsInsidePolygon(IReadOnlyList<LayoutPoint> points, float u, float v)
	{
		var inside = false;
		for (var i = 0; i < points.Count; i++)
		{
			var a = points[i];
			var b = points[(i + points.Count - 1) % points.Count];
			if ((a.Y > v) == (b.Y > v))
				continue;

			var x = (b.X - a.X) * (v - a.Y) / (b.Y - a.Y) + a.X;
			if (u < x)
				inside = !inside;
		}

		return inside;
	}

	private static float DistanceToPolygon(IReadOnlyList<LayoutPoint> points, float u, float v)
	{
		var best = float.MaxValue;
		for (var i = 0; i < points.Count; i++)
		{
			var a = points[i];
			var b = points[(i + 1) % points.Count];
			best = MathF.Min(best, DistanceToSegment(u, v, a.X, a.Y, b.X, b.Y));
		}

		return best;
	}

	private static float DistanceToSegment(float px, float py, float ax, float ay, float bx, float by)
	{
		var dx = bx - ax;
		var dy = by - ay;
		var len2 = dx * dx + dy * dy;
		if (len2 <= 0.000001f)
			return MathF.Sqrt((px - ax) * (px - ax) + (py - ay) * (py - ay));

		var t = Math.Clamp(((px - ax) * dx + (py - ay) * dy) / len2, 0f, 1f);
		var x = ax + dx * t;
		var y = ay + dy * t;
		return MathF.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
	}

	private static float EdgeValue(float signedDistance, float falloff)
	{
		if (falloff <= 0.0001f)
			return signedDistance >= 0f ? 1f : 0f;

		return Smooth01(Math.Clamp(signedDistance / falloff, 0f, 1f));
	}

	private static float Smooth01(float value)
	{
		value = Math.Clamp(value, 0f, 1f);
		return value * value * (3f - 2f * value);
	}
}
