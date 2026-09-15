
using NovoDwarf.Primitives.Models;
using NovoDwarf.Primitives.Models.Float2;
using NovoDwarf.Primitives.Models.Int2;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Spatial;

public sealed class ConnectPointsNode : BaseNode, IPathSetExecutor
{
	public override string Key => NodeKeys.ConnectPoints;
	public override string GroupKey => NodeGroupKeys.Utilities;
	public override ColorF Color => new(0.36f, 0.44f, 0.22f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.Points, ValueShape.Points, PortSemantics.Position),
		new(PortKeys.Cost, ValueShape.Any)
	];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.Paths, ValueShape.Paths, PortSemantics.Generic)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.InputCount, 1f, 8f, 1f, 2f, UseSlider: false, Category: ParameterCategory.Primary),
		new(ParameterKeys.MaxDistance, 1f, 256f, 1f, 32f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Radius, 0.5f, 16f, 0.5f, 2f, Category: ParameterCategory.Advanced)
	];

	public PathSet EvaluatePaths(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var points = graph.RequirePointSetInput(model, PortKeys.Points, width, height);
		if (points.Points.Length <= 1)
			return PathSet.Empty(width, height);

		var costField = ResolveOptionalFieldInput(graph, model, PortKeys.Cost, width, height);
		var neighborCount = Math.Max(1, graph.GetParameter<int>(model, ParameterKeys.InputCount, 0.5f, 0.5f));
		var maxDistance = Math.Max(0.01f, graph.GetParameter(model, ParameterKeys.MaxDistance, 0.5f, 0.5f));
		var roadWidth = Math.Max(0.5f, graph.GetParameter(model, ParameterKeys.Radius, 0.5f, 0.5f));

		var edges = new HashSet<(int A, int B)>();
		var paths = new List<SpatialPath>();

		for (var i = 0; i < points.Points.Length; i++)
		{
			var i1 = i;
			
			var ordered = points.Points
			                    .Select((point, index) => new { Point = point, Index = index, Distance = Distance(points.Points[i].Position, point.Position) })
			                    .Where(candidate => candidate.Index != i1 && candidate.Distance <= maxDistance)
			                    .OrderBy(candidate => candidate.Distance)
			                    .Take(neighborCount);

			foreach (var candidate in ordered)
			{
				var a = Math.Min(i, candidate.Index);
				var b = Math.Max(i, candidate.Index);
				if (!edges.Add((a, b)))
					continue;

				var polyline = costField is null
					? [points.Points[a].Position, points.Points[b].Position]
					: FindPath(points.Points[a].Position, points.Points[b].Position, costField);

				if (polyline.Count >= 2)
					paths.Add(new SpatialPath(polyline, roadWidth, 1f));
			}
		}

		return new PathSet(width, height, paths);
	}

	private static List<Float2> FindPath(Float2 start, Float2 goal, Field costField)
	{
		var width = costField.Width;
		var height = costField.Height;
		var startCell = ToCell(start, width, height);
		var goalCell = ToCell(goal, width, height);

		var frontier = new PriorityQueue<Int2, float>();
		var cameFrom = new Dictionary<Int2, Int2>();
		var costSoFar = new Dictionary<Int2, float> { [startCell] = 0f };
		frontier.Enqueue(startCell, 0f);

		while (frontier.TryDequeue(out var current, out _))
		{
			if (current == goalCell)
				break;

			foreach (var next in EnumerateNeighbors(current, width, height))
			{
				if (IsDiagonal(current, next) && !HasDiagonalClearance(current, next, width, height))
					continue;

				var cellCost = 1f + Math.Max(0f, costField[next.X, next.Y]) * 4f;
				var newCost = costSoFar[current] + cellCost;

				if (costSoFar.TryGetValue(next, out var existing) && newCost >= existing)
					continue;

				costSoFar[next] = newCost;
				cameFrom[next] = current;
				frontier.Enqueue(next, newCost + Heuristic(next, goalCell));
			}
		}

		if (!cameFrom.ContainsKey(goalCell))
			return [start, goal];

		var cells = new List<Int2> { goalCell };
		var cursor = goalCell;
		while (cameFrom.TryGetValue(cursor, out var previous))
		{
			cells.Add(previous);
			cursor = previous;
		}

		cells.Reverse();
		return Simplify(cells.Select(cell => new Float2(cell.X + 0.5f, cell.Y + 0.5f)).ToList());
	}

	private static List<Float2> Simplify(List<Float2> points)
	{
		if (points.Count <= 2)
			return points;

		var result = new List<Float2> { points[0] };

		for (var i = 1; i < points.Count - 1; i++)
		{
			var a = result[^1];
			var b = points[i];
			var c = points[i + 1];
			var ab = b - a;
			var bc = c - b;
			if (MathF.Abs(ab.X * bc.Y - ab.Y * bc.X) < 0.001f)
				continue;

			result.Add(b);
		}

		result.Add(points[^1]);
		return result;
	}

	private static IEnumerable<Int2> EnumerateNeighbors(Int2 cell, int width, int height)
	{
		for (var dy = -1; dy <= 1; dy++)
		for (var dx = -1; dx <= 1; dx++)
		{
			if (dx == 0 && dy == 0)
				continue;

			var x = cell.X + dx;
			var y = cell.Y + dy;
			if (x < 0 || y < 0 || x >= width || y >= height)
				continue;

			yield return new Int2(x, y);
		}
	}

	private static bool HasDiagonalClearance(Int2 from, Int2 to, int width, int height)
	{
		var horizontal = new Int2(to.X, from.Y);
		var vertical = new Int2(from.X, to.Y);
		return IsInside(horizontal, width, height) && IsInside(vertical, width, height);
	}

	private static bool IsInside(Int2 cell, int width, int height) =>
		cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height;

	private static bool IsDiagonal(Int2 a, Int2 b) => a.X != b.X && a.Y != b.Y;

	private static Int2 ToCell(Float2 point, int width, int height) =>
		new(
			Math.Clamp((int)MathF.Round(point.X), 0, width - 1),
			Math.Clamp((int)MathF.Round(point.Y), 0, height - 1));

	private static float Heuristic(Int2 a, Int2 b) =>
		MathF.Abs(a.X - b.X) + MathF.Abs(a.Y - b.Y);

	private static float Distance(Float2 a, Float2 b)
	{
		var dx = a.X - b.X;
		var dy = a.Y - b.Y;
		return MathF.Sqrt(dx * dx + dy * dy);
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
}
