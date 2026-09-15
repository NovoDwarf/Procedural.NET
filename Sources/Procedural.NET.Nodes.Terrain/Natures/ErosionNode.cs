
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Terrain.Natures;

public sealed class ErosionNode : BaseNode, ISampledExecutor, IFieldExecutor
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
		new(ParameterKeys.Strength, 0f, 1f, 0.01f, 0.55f, Category: ParameterCategory.Primary),
		new(PortKeys.Flow, 0f, 1f, 0.01f, 0.65f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Talus, 0f, 1f, 0.01f, 0.35f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Scale, 2f, 64f, 1f, 14f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Seed, 0f, 100000f, 1f, 34117f, UseSlider: false, Category: ParameterCategory.Primary),
		new(ParameterKeys.Iterations, 1000f, 200000f, 1000f, 50000f, UseSlider: false),
		new(ParameterKeys.MaxLifetime, 1f, 200f, 1f, 40f, UseSlider: false),
		new(ParameterKeys.ErosionRadius, 1f, 16f, 1f, 3f),
		new(ParameterKeys.Inertia, 0f, 1f, 0.01f, 0.05f),
		new(ParameterKeys.SedimentCapacity, 0.001f, 16f, 0.01f, 4f),
		new(ParameterKeys.MinSedimentCapacity, 0.00001f, 1f, 0.0001f, 0.01f),
		new(ParameterKeys.ErodeSpeed, 0f, 1f, 0.01f, 0.3f),
		new(ParameterKeys.DepositSpeed, 0f, 1f, 0.01f, 0.3f),
		new(ParameterKeys.EvaporateSpeed, 0f, 0.1f, 0.001f, 0.01f),
		new(ParameterKeys.Gravity, 0.001f, 20f, 0.1f, 4f),
		new(ParameterKeys.InitialWater, 0.001f, 4f, 0.01f, 1f),
		new(ParameterKeys.InitialSpeed, 0.001f, 4f, 0.01f, 1f)
	];
	
	public override string Key => NodeKeys.Erosion;
	public override string GroupKey => NodeGroupKeys.Nature;
	
	public override ColorF Color => new(0.28f, 0.44f, 0.30f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
		=> EvaluateScalar(model, graph, context);

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var source = graph.RequireFieldInput(model, PortKeys.Source, width, height);
		var result = CloneBuffer(source, width, height);

		var seed = graph.GetParameter<int>(model, ParameterKeys.Seed, 0.5f, 0.5f);
		var strength = Math.Clamp(graph.GetParameter(model, ParameterKeys.Strength, 0.5f, 0.5f), 0f, 1f);

		if (strength <= 0f)
			return result;

		var settings = ReadSettings(model, graph);

		ApplyHydraulicErosion(result, width, height, settings, seed, strength);

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

	private static HydraulicErosionSettings ReadSettings(GraphNode model, IGraphExecutionContext graph)
	{
		return new HydraulicErosionSettings
		{
			Iterations = Math.Max(1, graph.GetParameter<int>(model, ParameterKeys.Iterations, 0.5f, 0.5f)),
			MaxLifetime = Math.Max(1, graph.GetParameter<int>(model, ParameterKeys.MaxLifetime, 0.5f, 0.5f)),
			ErosionRadius = Math.Clamp(graph.GetParameter<int>(model, ParameterKeys.ErosionRadius, 0.5f, 0.5f), 1, 16),

			Inertia = Math.Clamp(graph.GetParameter(model, ParameterKeys.Inertia, 0.5f, 0.5f), 0f, 1f),
			SedimentCapacityFactor = MathF.Max(0.001f, graph.GetParameter(model, ParameterKeys.SedimentCapacity, 0.5f, 0.5f)),
			MinSedimentCapacity = MathF.Max(0.00001f, graph.GetParameter(model, ParameterKeys.MinSedimentCapacity, 0.5f, 0.5f)),

			ErodeSpeed = Math.Clamp(graph.GetParameter(model, ParameterKeys.ErodeSpeed, 0.5f, 0.5f), 0f, 1f),
			DepositSpeed = Math.Clamp(graph.GetParameter(model, ParameterKeys.DepositSpeed, 0.5f, 0.5f), 0f, 1f),
			EvaporateSpeed = Math.Clamp(graph.GetParameter(model, ParameterKeys.EvaporateSpeed, 0.5f, 0.5f), 0f, 1f),
			Gravity = MathF.Max(0.001f, graph.GetParameter(model, ParameterKeys.Gravity, 0.5f, 0.5f)),

			InitialWater = MathF.Max(0.001f, graph.GetParameter(model, ParameterKeys.InitialWater, 0.5f, 0.5f)),
			InitialSpeed = MathF.Max(0.001f, graph.GetParameter(model, ParameterKeys.InitialSpeed, 0.5f, 0.5f))
		};
	}

	private static void ApplyHydraulicErosion(
		Field heightMap,
		int width,
		int height,
		HydraulicErosionSettings settings,
		int seed,
		float strength)
	{
		if (width < 3 || height < 3)
			return;

		var random = new Random(seed);

		for (var i = 0; i < settings.Iterations; i++)
		{
			var posX = 1f + (float)random.NextDouble() * (width - 3);
			var posY = 1f + (float)random.NextDouble() * (height - 3);

			var dirX = 0f;
			var dirY = 0f;
			var speed = settings.InitialSpeed;
			var water = settings.InitialWater;
			var sediment = 0f;

			for (var lifetime = 0; lifetime < settings.MaxLifetime; lifetime++)
			{
				var cellX = (int)posX;
				var cellY = (int)posY;

				if (cellX < 0 || cellX >= width - 1 || cellY < 0 || cellY >= height - 1)
					break;

				var offsetX = posX - cellX;
				var offsetY = posY - cellY;

				var current = SampleHeightAndGradient(heightMap, width, height, posX, posY);

				dirX = dirX * settings.Inertia - current.GradientX * (1f - settings.Inertia);
				dirY = dirY * settings.Inertia - current.GradientY * (1f - settings.Inertia);

				var dirLength = MathF.Sqrt(dirX * dirX + dirY * dirY);
				if (dirLength <= 0.0001f)
					break;

				dirX /= dirLength;
				dirY /= dirLength;

				var newX = posX + dirX;
				var newY = posY + dirY;

				if (newX < 0f || newX >= width - 1 || newY < 0f || newY >= height - 1)
					break;

				var newHeight = SampleHeightAndGradient(heightMap, width, height, newX, newY).Height;
				var deltaHeight = newHeight - current.Height;

				var capacity = MathF.Max(
					-deltaHeight * speed * water * settings.SedimentCapacityFactor,
					settings.MinSedimentCapacity);

				if (sediment > capacity || deltaHeight > 0f)
				{
					var depositAmount = deltaHeight > 0f
						? MathF.Min(deltaHeight, sediment)
						: (sediment - capacity) * settings.DepositSpeed;

					depositAmount *= strength;
					sediment -= depositAmount;

					DepositBilinear(heightMap, cellX, cellY, offsetX, offsetY, depositAmount);
				}
				else
				{
					var erosionAmount = MathF.Min((capacity - sediment) * settings.ErodeSpeed, -deltaHeight);

					erosionAmount *= strength;

					if (erosionAmount > 0f)
					{
						var removed = Erode(heightMap, width, height, posX, posY, settings.ErosionRadius, erosionAmount);
						sediment += removed;
					}
				}

				speed = MathF.Sqrt(MathF.Max(0f, speed * speed + deltaHeight * settings.Gravity));
				water *= 1f - settings.EvaporateSpeed;

				posX = newX;
				posY = newY;

				if (water <= 0.001f)
					break;
			}
		}

		ClampBuffer01(heightMap, width, height);
	}

	private static void DepositBilinear(Field map, int cellX, int cellY, float offsetX, float offsetY, float amount)
	{
		map[cellX, cellY]         += amount * (1f - offsetX) * (1f - offsetY);
		map[cellX + 1, cellY]     += amount * offsetX * (1f - offsetY);
		map[cellX, cellY + 1]     += amount * (1f - offsetX) * offsetY;
		map[cellX + 1, cellY + 1] += amount * offsetX * offsetY;
	}

	private static float Erode(Field map, int width, int height, float posX, float posY, int radius, float amount)
	{
		var centerX = (int)posX;
		var centerY = (int)posY;
		var weightSum = 0f;

		for (var y = -radius; y <= radius; y++)
		for (var x = -radius; x <= radius; x++)
		{
			var px = centerX + x;
			var py = centerY + y;
			if (px < 0 || px >= width || py < 0 || py >= height)
				continue;
			var distance = MathF.Sqrt(x * x + y * y);
			if (distance > radius) continue;
			weightSum += radius - distance;
		}

		if (weightSum <= 0f)
			return 0f;

		var removedTotal = 0f;
		for (var y = -radius; y <= radius; y++)
		for (var x = -radius; x <= radius; x++)
		{
			var px = centerX + x;
			var py = centerY + y;
			if (px < 0 || px >= width || py < 0 || py >= height)
				continue;
			var distance = MathF.Sqrt(x * x + y * y);
			if (distance > radius) continue;
			var weight = (radius - distance) / weightSum;
			var requested = amount * weight;
			var removed = MathF.Min(map[px, py], requested);
			map[px, py] -= removed;
			removedTotal += removed;
		}

		return removedTotal;
	}

	private readonly record struct HeightGradient(float Height, float GradientX, float GradientY);

	private static HeightGradient SampleHeightAndGradient(Field map, int width, int height, float x, float y)
	{
		var cellX = Math.Clamp((int)x, 0, width - 2);
		var cellY = Math.Clamp((int)y, 0, height - 2);
		var offsetX = x - cellX;
		var offsetY = y - cellY;

		var h00 = map[cellX, cellY];
		var h10 = map[cellX + 1, cellY];
		var h01 = map[cellX, cellY + 1];
		var h11 = map[cellX + 1, cellY + 1];

		var interpolatedHeight =
			h00 * (1f - offsetX) * (1f - offsetY) +
			h10 * offsetX * (1f - offsetY) +
			h01 * (1f - offsetX) * offsetY +
			h11 * offsetX * offsetY;

		var gradientX = (h10 - h00) * (1f - offsetY) + (h11 - h01) * offsetY;
		var gradientY = (h01 - h00) * (1f - offsetX) + (h11 - h10) * offsetX;

		return new HeightGradient(interpolatedHeight, gradientX, gradientY);
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

	private static float EvaluateScalar(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var seed = graph.GetParameter<int>(model, ParameterKeys.Seed, context);
		var strength = Math.Clamp(graph.GetParameter(model, ParameterKeys.Strength, context), 0f, 1f);
		var scale = MathF.Max(1f, graph.GetParameter(model, ParameterKeys.Scale, context));
		var flow = Math.Clamp(graph.GetParameter(model, ParameterKeys.Flow, context), 0f, 1f);
		var talus = Math.Clamp(graph.GetParameter(model, ParameterKeys.Talus, context), 0f, 1f);

		var baseHeight = graph.RequireScalarInput(model, PortKeys.Source, context);
		var step = 1f / (scale * 5f);

		var west = SampleHeight(model, graph, context with { U = context.U - step });
		var east = SampleHeight(model, graph, context with { U = context.U + step });
		var north = SampleHeight(model, graph, context with { V = context.V - step });
		var south = SampleHeight(model, graph, context with { V = context.V + step });

		var northwest = SampleHeight(model, graph, new EvaluationContext(context.U - step, context.V - step));
		var northeast = SampleHeight(model, graph, new EvaluationContext(context.U + step, context.V - step));
		var southwest = SampleHeight(model, graph, new EvaluationContext(context.U - step, context.V + step));
		var southeast = SampleHeight(model, graph, new EvaluationContext(context.U + step, context.V + step));

		var minNeighbor = Math.Min(
			Math.Min(Math.Min(west, east), Math.Min(north, south)),
			Math.Min(Math.Min(northwest, northeast), Math.Min(southwest, southeast)));

		var maxNeighbor = Math.Max(
			Math.Max(Math.Max(west, east), Math.Max(north, south)),
			Math.Max(Math.Max(northwest, northeast), Math.Max(southwest, southeast)));

		var dx = east - west;
		var dy = south - north;

		var slope      = Math.Clamp(MathF.Sqrt(dx * dx + dy * dy) * scale * 1.2f, 0f, 1f);
		var convexity  = Math.Clamp(baseHeight - (west + east + north + south) * 0.25f, 0f, 1f);
		var localRelief = Math.Clamp(maxNeighbor - minNeighbor, 0f, 1f);

		var valley = Math.Clamp((maxNeighbor - baseHeight) / Math.Max(0.001f, localRelief), 0f, 1f);
		var ridge  = Math.Clamp((baseHeight - minNeighbor) / Math.Max(0.001f, localRelief), 0f, 1f);

		var incision   = MathF.Pow(slope, 0.72f) * flow;
		var weathering = convexity * (0.35f + ridge * 0.65f);

		var eroded   = baseHeight - strength * (incision * 0.22f + weathering * 0.16f);
		var deposits = MathF.Pow(1f - slope, 2f) * valley * strength * talus * 0.18f;

		var value = Math.Clamp(eroded + deposits, 0f, 1f);

		if (!graph.TryScalarInput(model, PortKeys.Mask, context, out var maskValue))
			return value;

		maskValue = Math.Clamp(maskValue, 0f, 1f);
		return Math.Clamp(baseHeight * (1f - maskValue) + value * maskValue, 0f, 1f);
	}

	private static float SampleHeight(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var u = Math.Clamp(context.U, 0f, 1f);
		var v = Math.Clamp(context.V, 0f, 1f);
		return Math.Clamp(graph.RequireScalarInput(model, PortKeys.Source, new EvaluationContext(u, v)), 0f, 1f);
	}
}
