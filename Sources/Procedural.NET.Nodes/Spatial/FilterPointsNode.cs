
using NovoDwarf.Primitives.Models;
using NovoDwarf.Primitives.Models.Float2;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Spatial;

public sealed class FilterPointsNode : BaseNode, IPointSetExecutor
{
	public override string Key => NodeKeys.FilterPoints;
	public override string GroupKey => NodeGroupKeys.Utilities;
	
	public override ColorF Color => new(0.42f, 0.46f, 0.24f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.Points, ValueShape.Points, PortSemantics.Position),
		new(PortKeys.Mask, ValueShape.Any, PortSemantics.Mask)
	];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.Points, ValueShape.Points, PortSemantics.Position)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Threshold, 0f, 1f, 0.01f, 0.2f, Category: ParameterCategory.Primary),
		new(ParameterKeys.MinDistance, 0f, 128f, 0.5f, 0f, Category: ParameterCategory.Advanced)
	];

	public PointSet EvaluatePoints(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var source = graph.RequirePointSetInput(model, PortKeys.Points, width, height);
		
		if (source.Points.Length == 0)
			return source;

		var threshold = Math.Clamp(graph.GetParameter(model, ParameterKeys.Threshold, 0.5f, 0.5f), 0f, 1f);
		var minDistance = Math.Max(0f, graph.GetParameter(model, ParameterKeys.MinDistance, 0.5f, 0.5f));
		var scoreField = ResolveOptionalFieldInput(graph, model, PortKeys.Mask, width, height);
		
		if (scoreField is null)
			return source;

		var filtered = source.Points
			.Select(point => new ScoredPoint(point, Sample(scoreField, point.Position)))
			.Where(candidate => candidate.Score >= threshold)
			.OrderByDescending(candidate => candidate.Score)
			.ToList();

		if (minDistance <= 0f)
			return new PointSet(width, height, [.. filtered.Select(candidate => candidate.Point)]);

		var minDistanceSquared = minDistance * minDistance;
		var accepted = new List<SpatialPoint>(filtered.Count);
		
		foreach (var candidate in filtered)
		{
			if (accepted.Exists(point => Float2.DistanceSquared(point.Position, candidate.Point.Position) < minDistanceSquared))
				continue;

			accepted.Add(candidate.Point);
		}

		return new PointSet(width, height, [.. accepted]);
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

	private static float Sample(Field field, Float2 position)
	{
		var x = Math.Clamp((int)MathF.Round(position.X), 0, field.Width - 1);
		var y = Math.Clamp((int)MathF.Round(position.Y), 0, field.Height - 1);
		return field[x, y];
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

	private readonly record struct ScoredPoint(SpatialPoint Point, float Score);
}
