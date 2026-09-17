
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Terrain.Natures;

public sealed class LakeNode : BaseNode, ISampledExecutor, IFieldExecutor, IMultiFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs = 
	[
		new(PortKeys.Source, ValueShape.Field, PortSemantics.Terrain)
	];
	
	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs = 
	[
		new(PortKeys.C, ValueShape.Field, PortSemantics.Terrain),
		new(PortKeys.Mask, ValueShape.Field, PortSemantics.Water)
	];
	
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 
	[
		new(ParameterKeys.WaterLevel, 0f, 1f, 0.01f, 0.3f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Blend, 0f, 1f, 0.01f, 1f,
			Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.Lake;
	public override string GroupKey => NodeGroupKeys.Nature;
	
	public override ColorF Color => new(0.22f, 0.38f, 0.52f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var source = graph.RequireScalarInput(model, PortKeys.Source, context);
		var level  = graph.GetParameter(model, ParameterKeys.WaterLevel, context);
		var blend  = graph.GetParameter(model, ParameterKeys.Blend, context);
		return Math.Clamp(source * (1f - blend) + MathF.Max(source, level) * blend, 0f, 1f);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
		=> EvaluateField(model, graph, PortKeys.C, width, height);

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height)
	{
		var source = graph.RequireFieldInput(model, PortKeys.Source, width, height);
		var level  = Math.Clamp(graph.GetParameter(model, ParameterKeys.WaterLevel, 0.5f, 0.5f), 0f, 1f);
		var blend  = Math.Clamp(graph.GetParameter(model, ParameterKeys.Blend, 0.5f, 0.5f), 0f, 1f);

		var filled = FillDepressions(source, width, height, level);
		var result = new Field(width, height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var original = source[x, y];
			var f        = filled[y * width + x];
			result[x, y] = outputKey == PortKeys.Mask
				? WaterMask(original, f, level)
				: Math.Clamp(original * (1f - blend) + f * blend, 0f, 1f);
		}

		return result;
	}

	private static float WaterMask(float original, float filled, float level)
	{
		if (filled <= original)
			return 0f;

		return Math.Clamp((filled - original) / Math.Max(level, 0.001f), 0f, 1f);
	}

	private static float[] FillDepressions(Field source, int width, int height, float level)
	{
		var result = new float[width * height];
		for (var i = 0; i < result.Length; i++)
			result[i] = source[i % width, i / width];

		if (level <= 0f)
			return result;

		var open  = new bool[width * height];
		var queue = new Queue<int>();

		for (var x = 0; x < width; x++)
		{
			TryEnqueue(x, 0,          width, height, source, level, open, queue);
			TryEnqueue(x, height - 1, width, height, source, level, open, queue);
		}
		for (var y = 1; y < height - 1; y++)
		{
			TryEnqueue(0,         y, width, height, source, level, open, queue);
			TryEnqueue(width - 1, y, width, height, source, level, open, queue);
		}

		while (queue.Count > 0)
		{
			var idx = queue.Dequeue();
			var cx = idx % width;
			var cy = idx / width;
			TryEnqueue(cx - 1, cy, width, height, source, level, open, queue);
			TryEnqueue(cx + 1, cy, width, height, source, level, open, queue);
			TryEnqueue(cx, cy - 1, width, height, source, level, open, queue);
			TryEnqueue(cx, cy + 1, width, height, source, level, open, queue);
		}

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var idx = y * width + x;
			if (source[x, y] < level && !open[idx])
				result[idx] = level;
		}

		return result;
	}

	private static void TryEnqueue(int x, int y, int width, int height, Field source, float level, bool[] open, Queue<int> queue)
	{
		if (x < 0 || x >= width || y < 0 || y >= height)
			return;

		var idx = y * width + x;
		if (open[idx] || source[x, y] >= level)
			return;

		open[idx] = true;
		queue.Enqueue(idx);
	}
}
