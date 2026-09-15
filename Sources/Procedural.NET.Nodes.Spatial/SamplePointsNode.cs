
using NovoDwarf.Primitives.Models;
using NovoDwarf.Primitives.Models.Float2;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Spatial;

public sealed class SamplePointsNode : BaseNode, IPointSetExecutor
{
	public override string Key => NodeKeys.SamplePoints;
	public override string GroupKey => NodeGroupKeys.Generators;
	public override ColorF Color => new(0.36f, 0.50f, 0.28f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.Source, ValueShape.Any),
		new(PortKeys.Mask, ValueShape.Any, PortSemantics.Mask)
	];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.Points, ValueShape.Points, PortSemantics.Position)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Seed, 0f, 999999f, 1f, 1337f, UseSlider: false, Category: ParameterCategory.Primary),
		new(ParameterKeys.PointCount, 1f, 512f, 1f, 32f, UseSlider: false, Category: ParameterCategory.Primary),
		new(ParameterKeys.Threshold, 0f, 1f, 0.01f, 0.1f, Category: ParameterCategory.Primary),
		new(ParameterKeys.MinDistance, 0f, 128f, 0.5f, 4f, Category: ParameterCategory.Advanced),
		new(ParameterKeys.Jitter, 0f, 1f, 0.01f, 0.35f, Category: ParameterCategory.Advanced)
	];

	public PointSet EvaluatePoints(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var density = ResolveFieldInput(graph, model, PortKeys.Source, width, height, 0f);
		var mask = ResolveOptionalFieldInput(graph, model, PortKeys.Mask, width, height);

		var threshold = Math.Clamp(graph.GetParameter(model, ParameterKeys.Threshold, 0.5f, 0.5f), 0f, 1f);
		var minDistance = Math.Max(0f, graph.GetParameter(model, ParameterKeys.MinDistance, 0.5f, 0.5f));
		var jitter = Math.Clamp(graph.GetParameter(model, ParameterKeys.Jitter, 0.5f, 0.5f), 0f, 1f) * 0.5f;
		var targetCount = Math.Max(1, graph.GetParameter<int>(model, ParameterKeys.PointCount, 0.5f, 0.5f));
		var seed = graph.GetParameter<int>(model, ParameterKeys.Seed, 0.5f, 0.5f);

		var candidates = new List<Candidate>(width * height / 4);
		var totalWeight = 0f;

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var weight = density[x, y] * (mask?[x, y] ?? 1f);
			if (weight < threshold)
				continue;

			totalWeight += weight;
			candidates.Add(new Candidate(x, y, totalWeight));
		}

		if (candidates.Count == 0 || totalWeight <= 0f)
			return PointSet.Empty(width, height);

		var random = new Random(seed);
		var points = new List<SpatialPoint>(Math.Min(targetCount, candidates.Count));
		var attempts = Math.Max(targetCount * 8, candidates.Count);

		for (var attempt = 0; attempt < attempts && points.Count < targetCount; attempt++)
		{
			var hit = (float)(random.NextDouble() * totalWeight);
			var candidate = candidates[FindCandidateIndex(candidates, hit)];
			var offsetX = ((float)random.NextDouble() * 2f - 1f) * jitter;
			var offsetY = ((float)random.NextDouble() * 2f - 1f) * jitter;
			var position = new Float2(
				Math.Clamp(candidate.X + 0.5f + offsetX, 0f, width - 1f),
				Math.Clamp(candidate.Y + 0.5f + offsetY, 0f, height - 1f));

			if (minDistance > 0f && points.Exists(point => DistanceSquared(point.Position, position) < minDistance * minDistance))
				continue;

			points.Add(new SpatialPoint(position, 1f, Math.Max(1f, minDistance)));
		}

		return new PointSet(width, height,[.. points]);
	}

	private static int FindCandidateIndex(IReadOnlyList<Candidate> candidates, float hit)
	{
		var lo = 0;
		var hi = candidates.Count - 1;

		while (lo < hi)
		{
			var mid = (lo + hi) / 2;
			if (hit <= candidates[mid].CumulativeWeight)
				hi = mid;
			else
				lo = mid + 1;
		}

		return lo;
	}

	private static float DistanceSquared(Float2 a, Float2 b)
	{
		var dx = a.X - b.X;
		var dy = a.Y - b.Y;
		return dx * dx + dy * dy;
	}

	private static Field ResolveFieldInput(IGraphExecutionContext graph, GraphNode model, string portKey, int width, int height, float fallback)
	{
		if (!graph.TryValueInput(model, portKey, width, height, out var value) || value is null)
			return Fill(width, height, fallback);

		return value switch
		{
			GraphValue.Raster raster => raster.Value,
			GraphValue.Bitmap bitmap => ToField(bitmap.Value),
			GraphValue.Color color => Fill(width, height, (color.Value.R + color.Value.G + color.Value.B) / 3f),
			GraphValue.Scalar scalar => Fill(width, height, scalar.Value),
			_ => Fill(width, height, fallback)
		};
	}

	private static Field? ResolveOptionalFieldInput(IGraphExecutionContext graph, GraphNode model, string portKey, int width, int height)
	{
		if (!graph.TryValueInput(model, portKey, width, height, out var value) || value is null)
			return null;

		return value switch
		{
			GraphValue.Raster raster => raster.Value,
			GraphValue.Bitmap bitmap => ToField(bitmap.Value),
			GraphValue.Color color => Fill(width, height, (color.Value.R + color.Value.G + color.Value.B) / 3f),
			GraphValue.Scalar scalar => Fill(width, height, scalar.Value),
			_ => null
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

	private static Field ToField(RgbaBitmap bitmap)
	{
		var field = new Field(bitmap.Width, bitmap.Height);
		for (var y = 0; y < bitmap.Height; y++)
		for (var x = 0; x < bitmap.Width; x++)
			field[x, y] = (bitmap.R[x, y] + bitmap.G[x, y] + bitmap.B[x, y]) / 3f;

		return field;
	}

	private readonly record struct Candidate(int X, int Y, float CumulativeWeight);
}
