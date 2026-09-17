
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Terrain.Natures;

public sealed class ThermalErosionNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs = 
	[
		new(PortKeys.Source, ValueShape.Field, PortSemantics.Terrain),
		new(PortKeys.Mask, ValueShape.Field)
	];
	
	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs = 
	[
		new(PortKeys.C, ValueShape.Field, PortSemantics.Terrain)
	];
	
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 
	[
		new(ParameterKeys.Iterations, 1f, 200f, 1f, 50f,
			UseSlider: false, Category: ParameterCategory.Primary),
		new(ParameterKeys.Talus, 0.0001f, 1f, 0.001f, 0.1f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Strength, 0f, 1f, 0.01f, 0.5f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Scale, 2f, 64f, 1f, 14f,
			Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.ThermalErosion;
	public override string GroupKey => NodeGroupKeys.Nature;
	
	public override ColorF Color => new(0.28f, 0.44f, 0.30f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;


	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var baseHeight = graph.RequireScalarInput(model, PortKeys.Source, context);
		var talus      = Math.Clamp(graph.GetParameter(model, ParameterKeys.Talus, context), 0.0001f, 1f);
		var strength   = Math.Clamp(graph.GetParameter(model, ParameterKeys.Strength, context), 0f, 1f);
		var scale      = MathF.Max(1f, graph.GetParameter(model, ParameterKeys.Scale, context));

		var threshold = talus / scale;
		var step = 1f / (scale * 5f);

		var neighbors = new float[]
		{
			SampleHeight(model, graph, context with { U = context.U - step }),
			SampleHeight(model, graph, context with { U = context.U + step }),
			SampleHeight(model, graph, context with { V = context.V - step }),
			SampleHeight(model, graph, context with { V = context.V + step }),
			SampleHeight(model, graph, new EvaluationContext(context.U - step, context.V - step)),
			SampleHeight(model, graph, new EvaluationContext(context.U + step, context.V - step)),
			SampleHeight(model, graph, new EvaluationContext(context.U - step, context.V + step)),
			SampleHeight(model, graph, new EvaluationContext(context.U + step, context.V + step))
		};

		var minNeighbor = neighbors[0];
		for (var i = 1; i < neighbors.Length; i++)
			if (neighbors[i] < minNeighbor)
				minNeighbor = neighbors[i];

		var diff = baseHeight - minNeighbor;
		float value;

		if (diff > threshold)
			value = baseHeight - (diff - threshold) * 0.5f * strength;
		else
			value = baseHeight;

		value = Math.Clamp(value, 0f, 1f);

		if (!graph.TryScalarInput(model, PortKeys.Mask, context, out var maskValue))
			return value;

		maskValue = Math.Clamp(maskValue, 0f, 1f);
		return Math.Clamp(baseHeight * (1f - maskValue) + value * maskValue, 0f, 1f);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var source   = graph.RequireFieldInput(model, PortKeys.Source, width, height);
		var result   = CloneBuffer(source, width, height);

		var iterations = Math.Max(1, graph.GetParameter<int>(model, ParameterKeys.Iterations, 0.5f, 0.5f));
		var talus      = Math.Clamp(graph.GetParameter(model, ParameterKeys.Talus, 0.5f, 0.5f), 0.0001f, 1f);
		var strength   = Math.Clamp(graph.GetParameter(model, ParameterKeys.Strength, 0.5f, 0.5f), 0f, 1f);
		var scale      = MathF.Max(1f, graph.GetParameter(model, ParameterKeys.Scale, 0.5f, 0.5f));

		if (strength > 0f)
			ApplyThermalErosion(result, width, height, iterations, talus / scale, strength);

		if (!graph.TryFieldInput(model, PortKeys.Mask, width, height, out var maskBuffer))
			return result;

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var maskValue = Math.Clamp(maskBuffer![x, y], 0f, 1f);
			result[x, y] = Math.Clamp(
				source[x, y] * (1f - maskValue) + result[x, y] * maskValue, 0f, 1f);
		}

		return result;
	}

	private static readonly (int dx, int dy)[] Neighbors =
	[
		(-1, 0), (1, 0), (0, -1), (0, 1),
		(-1, -1), (1, -1), (-1, 1), (1, 1)
	];

	private static void ApplyThermalErosion(
		Field map,
		int width,
		int height,
		int iterations,
		float threshold,
		float strength)
	{
		for (var iter = 0; iter < iterations; iter++)
		{
			for (var y = 0; y < height; y++)
			for (var x = 0; x < width; x++)
			{
				var h = map[x, y];

				// Find steepest downhill neighbor
				var maxDiff  = 0f;
				var totalDiff = 0f;
				var lowestNx = -1;
				var lowestNy = -1;

				foreach (var (dx, dy) in Neighbors)
				{
					var nx = x + dx;
					var ny = y + dy;
					if (nx < 0 || nx >= width || ny < 0 || ny >= height)
						continue;

					var diff = h - map[nx, ny];
					if (diff <= threshold) continue;

					totalDiff += diff;
					if (diff > maxDiff)
					{
						maxDiff  = diff;
						lowestNx = nx;
						lowestNy = ny;
					}
				}

				if (lowestNx < 0 || totalDiff <= 0f)
					continue;

				var move = (maxDiff - threshold) * 0.5f * strength;
				map[x, y]             -= move;
				map[lowestNx, lowestNy] += move;
			}
		}

		ClampBuffer01(map, width, height);
	}

	private static Field CloneBuffer(Field source, int width, int height)
	{
		var result = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			result[x, y] = Math.Clamp(source[x, y], 0f, 1f);
		return result;
	}

	private static void ClampBuffer01(Field buffer, int width, int height)
	{
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			buffer[x, y] = Math.Clamp(buffer[x, y], 0f, 1f);
	}

	private static float SampleHeight(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var u = Math.Clamp(context.U, 0f, 1f);
		var v = Math.Clamp(context.V, 0f, 1f);
		return Math.Clamp(graph.RequireScalarInput(model, PortKeys.Source, new EvaluationContext(u, v)), 0f, 1f);
	}
}
