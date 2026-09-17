
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Terrain.Natures;

public sealed class CoastalErosionNode : BaseNode, IMultiSampledExecutor, IFieldExecutor, IMultiFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs = 
	[
		new(PortKeys.Source, ValueShape.Field, PortSemantics.Terrain)
	];
	
	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs = 
	[
		new(PortKeys.C, ValueShape.Field, PortSemantics.Terrain),
		new(PortKeys.WaterDepth, ValueShape.Field, PortSemantics.Water),
		new(PortKeys.Beach, ValueShape.Field, PortSemantics.Mask)
	];
	
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 
	[
		new(ParameterKeys.WaterLevel, 0f, 1f, 0.01f, 0.3f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.BeachSize, 0.001f, 0.5f, 0.001f, 0.08f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.InlandInfluence, 0f, 1f, 0.01f, 0.35f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.UnderwaterSmoothing, 0f, 8f, 1f, 3f,
			UseSlider: false, Category: ParameterCategory.Primary)
	];
	
	private const float FlatEpsilon = 0.0001f;

	public override string Key => NodeKeys.CoastalErosion;
	public override string GroupKey => NodeGroupKeys.Nature;
	
	public override ColorF Color => new(0.20f, 0.42f, 0.46f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, string outputKey, EvaluationContext context)
	{
		var source = Math.Clamp(graph.RequireScalarInput(model, PortKeys.Source, context), 0f, 1f);
		var level  = Math.Clamp(graph.GetParameter(model, ParameterKeys.WaterLevel, context), 0f, 1f);
		var beach  = Math.Clamp(graph.GetParameter(model, ParameterKeys.BeachSize, context), 0.001f, 0.5f);
		var inland = Math.Clamp(graph.GetParameter(model, ParameterKeys.InlandInfluence, context), 0f, 1f);
		var ocean  = source <= level + FlatEpsilon ? Math.Clamp((level - source) / Math.Max(level, 0.001f), 0.08f, 1f) : 0f;
		var shore  = BeachMask(source, level, beach, inland);
		var eroded = ErodeHeight(source, level, inland, ocean, shore);

		return outputKey switch
		{
			PortKeys.WaterDepth => WaterDepth(eroded, level, beach, ocean),
			PortKeys.Beach => shore,
			_ => eroded
		};
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
		=> EvaluateField(model, graph, PortKeys.C, width, height);

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height)
	{
		var source     = graph.RequireFieldInput(model, PortKeys.Source, width, height);
		var level      = Math.Clamp(graph.GetParameter(model, ParameterKeys.WaterLevel, 0.5f, 0.5f), 0f, 1f);
		var beach      = Math.Clamp(graph.GetParameter(model, ParameterKeys.BeachSize, 0.5f, 0.5f), 0.001f, 0.5f);
		var inland     = Math.Clamp(graph.GetParameter(model, ParameterKeys.InlandInfluence, 0.5f, 0.5f), 0f, 1f);
		var smoothing  = Math.Clamp(graph.GetParameter<int>(model, ParameterKeys.UnderwaterSmoothing, 0.5f, 0.5f), 0, 8);
		var oceanMask  = BuildOceanMask(source, width, height, level);
		var beachMask  = BuildBeachMask(source, oceanMask, width, height, level, beach, inland);
		var eroded     = BuildErodedTerrain(source, oceanMask, beachMask, width, height, level, inland, smoothing);
		var result     = new Field(width, height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			result[x, y] = outputKey switch
			{
				PortKeys.WaterDepth => WaterDepth(eroded[x, y], level, beach, oceanMask[x, y]),
				PortKeys.Beach => beachMask[x, y],
				_ => eroded[x, y]
			};
		}

		return result;
	}

	private static Field BuildErodedTerrain(
		Field source,
		Field oceanMask,
		Field beachMask,
		int width,
		int height,
		float level,
		float inland,
		int smoothing)
	{
		var current = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			current[x, y] = ErodeHeight(Math.Clamp(source[x, y], 0f, 1f), level, inland, oceanMask[x, y], beachMask[x, y]);

		for (var i = 0; i < smoothing; i++)
			current = SmoothUnderwater(current, oceanMask, width, height, level);

		return current;
	}

	private static Field SmoothUnderwater(Field source, Field oceanMask, int width, int height, float level)
	{
		var result = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var center = source[x, y];
			var mask = oceanMask[x, y];
			if (mask <= 0f)
			{
				result[x, y] = center;
				continue;
			}

			var sum = center;
			var count = 1;
			for (var oy = -1; oy <= 1; oy++)
			for (var ox = -1; ox <= 1; ox++)
			{
				if (ox == 0 && oy == 0)
					continue;

				var nx = Math.Clamp(x + ox, 0, width - 1);
				var ny = Math.Clamp(y + oy, 0, height - 1);
				sum += source[nx, ny];
				count++;
			}

			var target = Math.Min(level, sum / count);
			result[x, y] = Math.Clamp(center * (1f - mask) + target * mask, 0f, 1f);
		}

		return result;
	}

	private static float ErodeHeight(float height, float level, float inland, float ocean, float beach)
	{
		if (ocean > 0f)
		{
			var depth = Math.Max(0f, level - height);
			var shelf = level - depth * 0.28f;
			return Math.Clamp(height * (1f - ocean) + shelf * ocean, 0f, 1f);
		}

		if (beach <= 0f)
			return height;

		var signed = Math.Max(0f, height - level);
		var target = level + signed * (1f - (0.45f + inland * 0.35f));
		return Math.Clamp(height * (1f - beach) + target * beach, 0f, 1f);
	}

	private static float WaterDepth(float height, float level, float beach, float ocean)
		=> ocean <= 0f
			? 0f
			: Math.Clamp(((level - height) / Math.Max(beach, 0.001f)) * ocean, 0f, 1f);

	private static float BeachMask(float height, float level, float beach, float inland)
	{
		var aboveRange = beach * (0.35f + inland * 1.65f);
		var belowRange = beach;
		var distance = height >= level
			? (height - level) / aboveRange
			: (level - height) / belowRange;

		return Smooth01(1f - Math.Clamp(distance, 0f, 1f));
	}

	private static float Smooth01(float value)
	{
		value = Math.Clamp(value, 0f, 1f);
		return value * value * (3f - 2f * value);
	}

	private static Field BuildOceanMask(Field source, int width, int height, float level)
	{
		var mask = new Field(width, height);
		var visited = new bool[width * height];
		var queue = new Queue<(int X, int Y)>();

		void EnqueueIfOcean(int x, int y)
		{
			var index = y * width + x;
			if (visited[index] || source[x, y] > level + FlatEpsilon)
				return;

			visited[index] = true;
			queue.Enqueue((x, y));
			mask[x, y] = Math.Clamp((level - source[x, y]) / Math.Max(level, 0.001f), 0.08f, 1f);
		}

		for (var x = 0; x < width; x++)
		{
			EnqueueIfOcean(x, 0);
			EnqueueIfOcean(x, height - 1);
		}

		for (var y = 1; y < height - 1; y++)
		{
			EnqueueIfOcean(0, y);
			EnqueueIfOcean(width - 1, y);
		}

		while (queue.Count > 0)
		{
			var (x, y) = queue.Dequeue();
			for (var oy = -1; oy <= 1; oy++)
			for (var ox = -1; ox <= 1; ox++)
			{
				if (ox == 0 && oy == 0)
					continue;

				var nx = x + ox;
				var ny = y + oy;
				if (nx < 0 || ny < 0 || nx >= width || ny >= height)
					continue;

				EnqueueIfOcean(nx, ny);
			}
		}

		return mask;
	}

	private static Field BuildBeachMask(Field source, Field oceanMask, int width, int height, float level, float beach, float inland)
	{
		var result = new Field(width, height);
		var maxDistance = Math.Max(2, (int)MathF.Ceiling((beach * (0.75f + inland * 2.5f)) * Math.Max(width, height)));
		var distance = new int[width * height];
		Array.Fill(distance, -1);
		var queue = new Queue<(int X, int Y)>();

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			if (oceanMask[x, y] > 0f)
				continue;

			if (!TouchesOcean(oceanMask, x, y))
				continue;

			distance[y * width + x] = 0;
			queue.Enqueue((x, y));
		}

		while (queue.Count > 0)
		{
			var (x, y) = queue.Dequeue();
			var currentDistance = distance[y * width + x];
			if (currentDistance >= maxDistance)
				continue;

			for (var oy = -1; oy <= 1; oy++)
			for (var ox = -1; ox <= 1; ox++)
			{
				if (ox == 0 && oy == 0)
					continue;

				var nx = x + ox;
				var ny = y + oy;
				if (nx < 0 || ny < 0 || nx >= width || ny >= height || oceanMask[nx, ny] > 0f)
					continue;

				var index = ny * width + nx;
				if (distance[index] >= 0)
					continue;

				distance[index] = currentDistance + 1;
				queue.Enqueue((nx, ny));
			}
		}

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			if (oceanMask[x, y] > 0f)
			{
				result[x, y] = 0f;
				continue;
			}

			var dist = distance[y * width + x];
			if (dist < 0)
			{
				result[x, y] = 0f;
				continue;
			}

			var distanceMask = 1f - Math.Clamp(dist / (float)Math.Max(1, maxDistance), 0f, 1f);
			var heightMask = BeachMask(source[x, y], level, beach, inland);
			result[x, y] = Smooth01(distanceMask * heightMask);
		}

		return result;
	}

	private static bool TouchesOcean(Field oceanMask, int x, int y)
	{
		for (var oy = -1; oy <= 1; oy++)
		for (var ox = -1; ox <= 1; ox++)
		{
			if (ox == 0 && oy == 0)
				continue;

			var nx = x + ox;
			var ny = y + oy;
			if (nx < 0 || ny < 0 || nx >= oceanMask.Width || ny >= oceanMask.Height)
				continue;

			if (oceanMask[nx, ny] > 0f)
				return true;
		}

		return false;
	}
}
