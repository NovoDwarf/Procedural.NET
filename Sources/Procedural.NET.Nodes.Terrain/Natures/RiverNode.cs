
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Terrain.Natures;

public sealed class RiverNode : BaseNode, ISampledExecutor, IFieldExecutor, IMultiFieldExecutor
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
		new(ParameterKeys.FlowThreshold, 0.001f, 1f, 0.001f, 0.05f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Depth, 0f, 1f, 0.01f, 0.3f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Smoothness, 0f, 1f, 0.01f, 0.5f,
			Category: ParameterCategory.Primary)
	];
	
	private const int SinkFillPasses = 8;
	private const float FlatFlowEpsilon = 0.0001f;

	public override string Key => NodeKeys.River;
	public override string GroupKey => NodeGroupKeys.Nature;
	
	public override ColorF Color => new(0.22f, 0.38f, 0.52f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var field = EvaluateField(model, graph, 16, 16);
		var x = (int)Math.Clamp(MathF.Round(context.U * 15), 0, 15);
		var y = (int)Math.Clamp(MathF.Round(context.V * 15), 0, 15);
		return field[x, y];
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
		=> EvaluateField(model, graph, PortKeys.C, width, height);

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height)
	{
		var source     = graph.RequireFieldInput(model, PortKeys.Source, width, height);
		var threshold  = Math.Clamp(graph.GetParameter(model, ParameterKeys.FlowThreshold, 0.5f, 0.5f), 0.001f, 1f);
		var depth      = Math.Clamp(graph.GetParameter(model, ParameterKeys.Depth,          0.5f, 0.5f), 0f, 1f);
		var smoothness = Math.Clamp(graph.GetParameter(model, ParameterKeys.Smoothness,     0.5f, 0.5f), 0f, 1f);

		using var hydrologySurface = BuildHydrologySurface(source, width, height);
		var flow = ComputeFlowAccumulation(hydrologySurface, source, width, height);

		var maxFlow = 1f;
		
		for (var i = 0; i < flow.Length; i++)
			if (flow[i] > maxFlow) maxFlow = flow[i];
	
		var invMax = 1f / maxFlow;

		using var rawMask = Field.Rent(width, height);
		var result = new Field(width, height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			rawMask[x, y] = ComputeRiverStrength(flow[y * width + x] * invMax, threshold);
		}

		var riverMask = SoftenRiverMask(rawMask, smoothness);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var original = source[x, y];
			var strength = riverMask[x, y];
			if (strength <= 0f)
			{
				result[x, y] = outputKey == PortKeys.Mask ? 0f : original;
				continue;
			}

			var carveStrength = Math.Max(rawMask[x, y], strength * 0.88f);
			var carveAmount = carveStrength * depth * (1f - smoothness * (1f - carveStrength));
			result[x, y] = outputKey == PortKeys.Mask
				? Math.Clamp(strength, 0f, 1f)
				: Math.Clamp(original - carveAmount, 0f, 1f);
		}

		return result;
	}

	// D8 flow routing to accumulation via topological sort (height-descending).
	private static float[] ComputeFlowAccumulation(Field routingSurface, Field source, int width, int height)
	{
		var n = width * height;
		var dir = new int[n];
		var accum = new float[n];

		Span<int> dx = [1, -1,  0,  0,  1, -1,  1, -1];
		Span<int> dy = [0,  0,  1, -1,  1,  1, -1, -1];
		Span<float> dist = [1f, 1f, 1f, 1f, 1.414f, 1.414f, 1.414f, 1.414f];

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var idx      = y * width + x;
			var h        = routingSurface[x, y];
			var bestDrop = 0f;
			var bestHeight = float.MaxValue;
			dir[idx]     = -1;
			accum[idx]   = 1f;

			for (var d = 0; d < 8; d++)
			{
				var nx = x + dx[d];
				var ny = y + dy[d];
				if (nx < 0 || nx >= width || ny < 0 || ny >= height)
					continue;

				var neighborHeight = routingSurface[nx, ny];
				var drop = (h - neighborHeight) / dist[d];
				if (drop > bestDrop + FlatFlowEpsilon)
				{
					bestDrop = drop;
					bestHeight = neighborHeight;
					dir[idx] = ny * width + nx;
				}
				else if (drop >= -FlatFlowEpsilon && neighborHeight < bestHeight - FlatFlowEpsilon)
				{
					bestHeight = neighborHeight;
					dir[idx] = ny * width + nx;
				}
			}
		}

		var order = new int[n];
		
		for (var i = 0; i < n; i++) 
			order[i] = i;
		
		Array.Sort(order, (a, b) =>
		{
			var ha = routingSurface[a % width, a / width];
			var hb = routingSurface[b % width, b / width];
		
			return hb.CompareTo(ha);
		});

		foreach (var idx in order)
		{
			var downstream = dir[idx];
			
			if (downstream < 0) 
				continue;
			
			var sx = idx % width;
			var sy = idx / width;
			var dxIndex = downstream % width;
			var dyIndex = downstream / width;
			var slope = Math.Max(0f, source[sx, sy] - source[dxIndex, dyIndex]);
			accum[downstream] += accum[idx] * (1f + slope * 2.2f);
		}

		return accum;
	}

	private static Field BuildHydrologySurface(Field source, int width, int height)
	{
		var result = Field.Rent(width, height);
		
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			result[x, y] = source[x, y];

		for (var pass = 0; pass < SinkFillPasses; pass++)
		{
			var changed = false;
			for (var y = 1; y < height - 1; y++)
			for (var x = 1; x < width - 1; x++)
			{
				var current = result[x, y];
				var minNeighbor = float.MaxValue;
				var hasLowerNeighbor = false;

				for (var oy = -1; oy <= 1; oy++)
				for (var ox = -1; ox <= 1; ox++)
				{
					if (ox == 0 && oy == 0)
						continue;

					var neighbor = result[x + ox, y + oy];
					minNeighbor = Math.Min(minNeighbor, neighbor);
					if (neighbor < current - FlatFlowEpsilon)
						hasLowerNeighbor = true;
				}

				if (hasLowerNeighbor || minNeighbor <= current + FlatFlowEpsilon)
					continue;

				result[x, y] = minNeighbor;
				changed = true;
			}

			if (!changed)
				break;
		}

		return result;
	}

	private static float ComputeRiverStrength(float normalizedFlow, float threshold)
	{
		if (normalizedFlow < threshold)
			return 0f;

		var strength = (normalizedFlow - threshold) / (1f - threshold);
		return Math.Clamp(MathF.Sqrt(Math.Clamp(strength, 0f, 1f)), 0f, 1f);
	}

	private static Field SoftenRiverMask(Field rawMask, float smoothness)
	{
		var width = rawMask.Width;
		var height = rawMask.Height;
		var result = new Field(width, height);
		var spread = 0.18f + smoothness * 0.30f;

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var center = rawMask[x, y];
			var weighted = center * 2.4f;
			var weight = 2.4f;
			var strongest = center;

			for (var oy = -1; oy <= 1; oy++)
			for (var ox = -1; ox <= 1; ox++)
			{
				if (ox == 0 && oy == 0)
					continue;

				var nx = Math.Clamp(x + ox, 0, width - 1);
				var ny = Math.Clamp(y + oy, 0, height - 1);
				var neighbor = rawMask[nx, ny];
				var localWeight = ox == 0 || oy == 0 ? 1f : 0.75f;
				weighted += neighbor * localWeight;
				weight += localWeight;
				strongest = Math.Max(strongest, neighbor * (ox == 0 || oy == 0 ? 0.92f : 0.78f));
			}

			var averaged = weighted / weight;
			var widened = Math.Max(center, strongest);
			result[x, y] = Math.Clamp(widened * (1f - spread) + averaged * spread, 0f, 1f);
		}

		return result;
	}
}
