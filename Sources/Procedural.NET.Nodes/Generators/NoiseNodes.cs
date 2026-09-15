
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Generators;

public abstract class NoiseNodeBase : BaseNode, ISampledExecutor, IFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs =
	[
		new(PortKeys.C, ValueShape.Field, PortSemantics.Terrain)
	];

	public override string GroupKey => NodeGroupKeys.Generators;
	public override string SubgroupKey => NodeSubgroupKeys.Noise;
	public override ColorF Color => new(0.22f, 0.44f, 0.52f);
	public override IReadOnlyList<GraphNodePort> Inputs => [];
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Frequency, 0.25f, 32f, 0.25f, 6f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Octaves, 1f, 8f, 1f, 4f, UseSlider: false, Category: ParameterCategory.Primary),
		new(ParameterKeys.Seed, 0f, 9999f, 1f, 1f, UseSlider: false),
		new(ParameterKeys.Gain, 0.1f, 1f, 0.05f, 0.5f),
		new(ParameterKeys.Lacunarity, 1f, 4f, 0.1f, 2f)
	];

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var frequency = MathF.Max(0.01f, graph.GetParameter(model, ParameterKeys.Frequency, context));
		var octaves = Math.Clamp(graph.GetParameter<int>(model, ParameterKeys.Octaves, context), 1, 8);
		var seed = graph.GetParameter<int>(model, ParameterKeys.Seed, context);
		var gain = Math.Clamp(graph.GetParameter(model, ParameterKeys.Gain, context), 0.01f, 1f);
		var lacunarity = MathF.Max(1f, graph.GetParameter(model, ParameterKeys.Lacunarity, context));

		var value = 0f;
		var amplitude = 1f;
		var amplitudeSum = 0f;
		for (var i = 0; i < octaves; i++)
		{
			value += Sample(context.U * frequency, context.V * frequency, seed + i * 31) * amplitude;
			amplitudeSum += amplitude;
			amplitude *= gain;
			frequency *= lacunarity;
		}

		return Math.Clamp(value / MathF.Max(0.0001f, amplitudeSum), 0f, 1f);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var result = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var u = width <= 1 ? 0f : x / (float)(width - 1);
			var v = height <= 1 ? 0f : y / (float)(height - 1);
			result[x, y] = EvaluatePoint(model, graph, new EvaluationContext(u, v));
		}

		return result;
	}

	protected abstract float Sample(float x, float y, int seed);

	protected static float Smooth(float value) => value * value * (3f - 2f * value);

	protected static float ValueAt(int x, int y, int seed)
	{
		var n = x * 374761393 + y * 668265263 + seed * 1442695041;
		n = (n ^ (n >> 13)) * 1274126177;
		return ((n ^ (n >> 16)) & 0x7fffffff) / (float)int.MaxValue;
	}

	protected static float Lerp(float a, float b, float t) => a + (b - a) * t;
}

public sealed class ValueNoiseNode : NoiseNodeBase
{
	public override string Key => NodeKeys.NoiseValue;

	protected override float Sample(float x, float y, int seed)
	{
		var x0 = (int)MathF.Floor(x);
		var y0 = (int)MathF.Floor(y);
		var tx = Smooth(x - x0);
		var ty = Smooth(y - y0);
		var a = Lerp(ValueAt(x0, y0, seed), ValueAt(x0 + 1, y0, seed), tx);
		var b = Lerp(ValueAt(x0, y0 + 1, seed), ValueAt(x0 + 1, y0 + 1, seed), tx);
		return Lerp(a, b, ty);
	}
}

public sealed class PerlinNoiseNode : NoiseNodeBase
{
	public override string Key => NodeKeys.NoisePerlin;

	protected override float Sample(float x, float y, int seed)
	{
		var value = MathF.Sin(x * 2.31f + seed * 0.17f) * MathF.Cos(y * 2.09f - seed * 0.11f);
		value += MathF.Sin((x + y) * 1.37f + seed * 0.07f) * 0.5f;
		return Math.Clamp(value * 0.25f + 0.5f, 0f, 1f);
	}
}

public sealed class VoronoiNoiseNode : NoiseNodeBase
{
	public override string Key => NodeKeys.NoiseVoronoi;

	protected override float Sample(float x, float y, int seed)
	{
		var cellX = (int)MathF.Floor(x);
		var cellY = (int)MathF.Floor(y);
		var nearest = float.MaxValue;

		for (var yy = -1; yy <= 1; yy++)
		for (var xx = -1; xx <= 1; xx++)
		{
			var px = cellX + xx + ValueAt(cellX + xx, cellY + yy, seed);
			var py = cellY + yy + ValueAt(cellX + xx, cellY + yy, seed + 17);
			var dx = px - x;
			var dy = py - y;
			nearest = MathF.Min(nearest, dx * dx + dy * dy);
		}

		return Math.Clamp(MathF.Sqrt(nearest), 0f, 1f);
	}
}
