using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution.Interfaces;
using Procedural.NET.Nodes.Terrain.Execution;
using Procedural.NET.Nodes.Terrain.Execution.Interfaces;

namespace Procedural.NET.Nodes.Terrain.Export;

public sealed class WorldExportNode : BaseNode, IWorldExportExecutor
{
	private const float WaterComponentThreshold = 0.12f;
	private const float WeakWaterThreshold = 0.16f;

	public override string Key => NodeKeys.WorldExport;
	public override string GroupKey => NodeGroupKeys.Export;
	
	public override ColorF Color => new(0.46f, 0.34f, 0.25f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.C,   ValueShape.Field,  PortSemantics.Terrain),
		new(PortKeys.Bitmap,      ValueShape.Bitmap, PortSemantics.Texture),
		new(PortKeys.SplatBitmap, ValueShape.Bitmap, PortSemantics.Texture),
		new(PortKeys.Points,      ValueShape.Points, PortSemantics.Position),
		new(PortKeys.Paths,       ValueShape.Paths,  PortSemantics.Generic),
		new(PortKeys.Water(1),    ValueShape.Field,  PortSemantics.Water, PortLocalizationKeys.Water(1)),
		new(PortKeys.Scatter(1),  ValueShape.Any,  PortSemantics.Generic, PortLocalizationKeys.Scatter(1))
	];

	public override IReadOnlyList<GraphNodePort> Outputs => [];

	public override IReadOnlyList<GraphNodeParameter> Parameters => BuildParameters(0, 1);

	public bool UpdatePorts(GraphNode model)
	{
		var waterInputs   = Math.Clamp(model.Get<int>(ParameterKeys.WaterInputs),   0, 8);
		var scatterInputs = Math.Clamp(model.Get<int>(ParameterKeys.ScatterInputs), 1, 16);
		var parametersChanged = model.ReplaceParameterDefinitions(BuildParameters(waterInputs, scatterInputs));
		var inputsChanged     = model.ReplaceInputs(CreateInputs(waterInputs, scatterInputs));
		return parametersChanged || inputsChanged;
	}

	public WorldExportResult EvaluateExport(GraphNode model, IGraphExecutionContext ctx, int resolution)
	{
		var elevation = ctx.RequireFieldInput(model, PortKeys.C, resolution, resolution);
		ctx.TryBitmapInput(model, PortKeys.Bitmap,      resolution, resolution, out var terrainPaintBitmap);
		ctx.TryBitmapInput(model, PortKeys.SplatBitmap, resolution, resolution, out var splatBitmapInput);
		ctx.TryPointSetInput(model, PortKeys.Points, resolution, resolution, out var points);
		ctx.TryPathSetInput(model, PortKeys.Paths, resolution, resolution, out var paths);

		var waterInputs   = Math.Clamp(model.Get<int>(ParameterKeys.WaterInputs),   0, 8);
		var scatterInputs = Math.Clamp(model.Get<int>(ParameterKeys.ScatterInputs), 1, 16);

		var waterLayers = new List<WaterSlot>(waterInputs);
		for (var i = 1; i <= waterInputs; i++)
			if (ctx.TryFieldInput(model, PortKeys.Water(i), resolution, resolution, out var f))
				waterLayers.Add(new WaterSlot(i, f!));

		var scatterLayers = new List<ScatterSlot>(scatterInputs);
		for (var i = 1; i <= scatterInputs; i++)
			if (ctx.TryFieldInput(model, PortKeys.Scatter(i), resolution, resolution, out var f))
				scatterLayers.Add(new ScatterSlot(i, f!, model.GetString(ParameterKeys.ScatterDefPath(i))));

		var seaLevel = Math.Clamp(ctx.Metadata.SeaLevel, 0f, 1f);
		var oceanMask = seaLevel > 0f
			? BuildOceanMask(elevation, seaLevel)
			: null;

		if (oceanMask is not null && waterLayers.Count > 0)
		{
			for (var i = 0; i < waterLayers.Count; i++)
				waterLayers[i] = new WaterSlot(waterLayers[i].Index, RemoveOceanCoverage(waterLayers[i].Mask, oceanMask, elevation, seaLevel));
		}

		if (waterLayers.Count > 0)
		{
			var minimumArea = Math.Max(18, (resolution * resolution) / 4096);
			for (var i = 0; i < waterLayers.Count; i++)
				waterLayers[i] = new WaterSlot(waterLayers[i].Index, RefineInlandWaterMask(waterLayers[i].Mask, minimumArea));
		}

		var normalMap = TerrainMapBuilder.BuildNormalMap(elevation);
		
		var combinedWaterMask = BuildCombinedWaterMask(elevation.Width, elevation.Height, waterLayers, oceanMask);
		var splatMap = splatBitmapInput ?? BuildFallbackSplatMap(elevation, scatterLayers);
		splatMap = ApplyWaterToSplatMap(splatMap, elevation, combinedWaterMask, oceanMask, seaLevel);

		terrainPaintBitmap = terrainPaintBitmap is not null 
			? ApplyWaterToPaintBitmap(terrainPaintBitmap, elevation, combinedWaterMask, oceanMask, seaLevel) 
			: BuildFallbackPaintBitmap(elevation, splatMap, combinedWaterMask);

		var topographicMap = TerrainMapBuilder.BuildTopographicMap(elevation);
		var aoMap          = TerrainMapBuilder.BuildAmbientOcclusionMap(elevation);
		var cavityMap      = TerrainMapBuilder.BuildCavityMap(elevation);
		var flowMap        = TerrainMapBuilder.BuildFlowMap(elevation);

		return new WorldExportResult
		{
			Elevation              = elevation,
			SeaLevel               = seaLevel,
			OceanMask              = oceanMask,
			Points                 = points,
			Paths                  = paths,
			WaterLayers            = waterLayers,
			ScatterLayers          = scatterLayers,
			TerrainPaintMapPath    = model.GetString(ParameterKeys.PaintMapPath),
			TerrainPaintBitmap     = terrainPaintBitmap,
			TopographicMap         = topographicMap,
			NormalMap              = normalMap,
			SplatMap               = splatMap,
			AmbientOcclusionMap    = aoMap,
			CavityMap              = cavityMap,
			FlowMap                = flowMap
		};
	}

	private static IReadOnlyList<GraphNodeParameter> BuildParameters(int waterInputs, int scatterInputs)
	{
		var parameters = new List<GraphNodeParameter>
		{
			new(ParameterKeys.PaintMapPath, 0f, 0f, 0f, 0f,
				Kind: ParameterKind.FilePath, Category: ParameterCategory.Primary),
			new(ParameterKeys.WaterInputs,   0f, 8f,  1f, 1f, UseSlider: false),
			new(ParameterKeys.ScatterInputs, 1f, 16f, 1f, 1f, UseSlider: false)
		};

		for (var i = 1; i <= scatterInputs; i++)
			parameters.Add(new GraphNodeParameter(
				ParameterKeys.ScatterDefPath(i),
				0f, 0f, 0f, 0f,
				Kind: ParameterKind.FilePath,
				Category: ParameterCategory.Primary));

		return parameters;
	}

	private static IEnumerable<GraphNodePort> CreateInputs(int waterInputs, int scatterInputs)
	{
		yield return new GraphNodePort(PortKeys.C,   ValueShape.Field,  PortSemantics.Terrain);
		yield return new GraphNodePort(PortKeys.Bitmap,      ValueShape.Bitmap, PortSemantics.Texture);
		yield return new GraphNodePort(PortKeys.SplatBitmap, ValueShape.Bitmap, PortSemantics.Texture);
		yield return new GraphNodePort(PortKeys.Points,      ValueShape.Points, PortSemantics.Position);
		yield return new GraphNodePort(PortKeys.Paths,       ValueShape.Paths,  PortSemantics.Generic);

		for (var i = 1; i <= waterInputs; i++)
			yield return new GraphNodePort(PortKeys.Water(i), ValueShape.Field, PortSemantics.Water, PortLocalizationKeys.Water(i));

		for (var i = 1; i <= scatterInputs; i++)
			yield return new GraphNodePort(PortKeys.Scatter(i), ValueShape.Any, PortSemantics.Generic, PortLocalizationKeys.Scatter(i));
	}

	private static RgbaBitmap BuildFallbackSplatMap(Field elevation, IReadOnlyList<ScatterSlot> scatterLayers)
	{
		if (scatterLayers.Count == 0)
			return TerrainMapBuilder.BuildProceduralSplatMap(elevation);

		var ordered = scatterLayers
			.OrderBy(static layer => layer.Index)
			.Take(4)
			.Select(static layer => layer.Density)
			.ToArray();

		return TerrainMapBuilder.AssembleSplatMap(
			ordered[0],
			ordered.Length > 1 ? ordered[1] : null,
			ordered.Length > 2 ? ordered[2] : null,
			ordered.Length > 3 ? ordered[3] : null);
	}

	private static Field BuildOceanMask(Field elevation, float seaLevel)
	{
		var mask = new Field(elevation.Width, elevation.Height);
		var visited = new bool[elevation.Width * elevation.Height];
		var queue = new Queue<(int X, int Z)>();

		void EnqueueIfOcean(int x, int z)
		{
			var index = z * elevation.Width + x;
			if (visited[index] || elevation[x, z] > seaLevel)
				return;

			visited[index] = true;
			queue.Enqueue((x, z));
			mask[x, z] = Math.Clamp((seaLevel - elevation[x, z]) / Math.Max(seaLevel, 0.0001f), 0.05f, 1f);
		}

		for (var x = 0; x < elevation.Width; x++)
		{
			EnqueueIfOcean(x, 0);
			EnqueueIfOcean(x, elevation.Height - 1);
		}

		for (var z = 1; z < elevation.Height - 1; z++)
		{
			EnqueueIfOcean(0, z);
			EnqueueIfOcean(elevation.Width - 1, z);
		}

		(int X, int Z)[] offsets =
		[
			(-1, 0), (1, 0), (0, -1), (0, 1)
		];

		while (queue.Count > 0)
		{
			var (x, z) = queue.Dequeue();
			foreach (var (ox, oz) in offsets)
			{
				var nx = x + ox;
				var nz = z + oz;
				if (nx < 0 || nz < 0 || nx >= elevation.Width || nz >= elevation.Height)
					continue;

				EnqueueIfOcean(nx, nz);
			}
		}

		return mask;
	}

	private static Field RemoveOceanCoverage(Field source, Field oceanMask, Field elevation, float seaLevel)
	{
		var result = new Field(source.Width, source.Height);
		for (var z = 0; z < source.Height; z++)
		for (var x = 0; x < source.Width; x++)
		{
			var ocean = Math.Clamp(oceanMask[x, z], 0f, 1f);
			result[x, z] = ocean > 0.01f
				? 0f
				: Math.Clamp(source[x, z] * (1f - ocean), 0f, 1f);
		}

		return result;
	}

	private static Field BuildCombinedWaterMask(int width, int height, IReadOnlyList<WaterSlot> waterLayers, Field? oceanMask)
	{
		var result = Field.Rent(width, height);
		
		for (var z = 0; z < height; z++)
		for (var x = 0; x < width; x++)
		{
			var value = 0f;
			
			foreach (var layer in waterLayers)
				value = Math.Max(value, layer.Mask[x, z]);

			if (oceanMask is not null)
				value = Math.Max(value, oceanMask[x, z]);

			result[x, z] = Math.Clamp(value, 0f, 1f);
		}

		return result;
	}

	private static RgbaBitmap ApplyWaterToPaintBitmap(RgbaBitmap source, Field elevation, Field waterMask, Field? oceanMask, float seaLevel)
	{
		var r = new Field(source.Width, source.Height);
		var g = new Field(source.Width, source.Height);
		var b = new Field(source.Width, source.Height);
		var a = new Field(source.Width, source.Height);

		for (var z = 0; z < source.Height; z++)
		for (var x = 0; x < source.Width; x++)
		{
			var water = Math.Clamp(waterMask[x, z], 0f, 1f);
			var ocean = oceanMask is null ? 0f : Math.Clamp(oceanMask[x, z], 0f, 1f);
			var inlandWater = Math.Max(0f, water - ocean);
			var coastal = oceanMask is null ? 0f : SampleCoastalInfluence(oceanMask, x, z);
			var wet = Math.Max(inlandWater * 0.75f, coastal * 0.35f);
			var baseR = source.R[x, z] + (0.34f - source.R[x, z]) * wet * 0.55f;
			var baseG = source.G[x, z] + (0.32f - source.G[x, z]) * wet * 0.55f;
			var baseB = source.B[x, z] + (0.25f - source.B[x, z]) * wet * 0.55f;

			if (ocean > 0.001f)
			{
				var depth = seaLevel > 0f
					? Math.Clamp((seaLevel - elevation[x, z]) / seaLevel, 0f, 1f)
					: 0f;
				var seabedR = 0.26f + (1f - depth) * 0.10f;
				var seabedG = 0.30f + (1f - depth) * 0.10f;
				var seabedB = 0.22f + (1f - depth) * 0.06f;
				var tint = 0.34f + ocean * 0.34f;
				r[x, z] = baseR + (seabedR - baseR) * tint;
				g[x, z] = baseG + (seabedG - baseG) * tint;
				b[x, z] = baseB + (seabedB - baseB) * tint;
			}
			else
			{
				var shoreBand = seaLevel > 0f
					? Math.Clamp(1f - ((elevation[x, z] - seaLevel) / Math.Max(0.08f, seaLevel * 0.35f)), 0f, 1f)
					: 0f;
				var shore = Math.Max(coastal * 0.85f, shoreBand * coastal);
				r[x, z] = baseR + (0.58f - baseR) * shore * 0.42f;
				g[x, z] = baseG + (0.53f - baseG) * shore * 0.42f;
				b[x, z] = baseB + (0.38f - baseB) * shore * 0.42f;
			}
			a[x, z] = source.A[x, z];
		}

		return new RgbaBitmap(r, g, b, a);
	}

	private static float SampleCoastalInfluence(Field oceanMask, int x, int z)
	{
		var influence = 0f;
		for (var oy = -2; oy <= 2; oy++)
		for (var ox = -2; ox <= 2; ox++)
		{
			var nx = Math.Clamp(x + ox, 0, oceanMask.Width - 1);
			var nz = Math.Clamp(z + oy, 0, oceanMask.Height - 1);
			var value = oceanMask[nx, nz];
			if (value <= 0f)
				continue;

			var distance = MathF.Sqrt(ox * ox + oy * oy);
			var weight = 1f / (1f + distance);
			influence = Math.Max(influence, value * weight);
		}

		return Math.Clamp(influence, 0f, 1f);
	}

	private static Field RefineInlandWaterMask(Field source, int minimumArea)
	{
		var suppressed = SuppressWeakWaterNoise(source);
		
		var result = new Field(source.Width, source.Height);
		for (var z = 0; z < source.Height; z++)
		for (var x = 0; x < source.Width; x++)
			result[x, z] = suppressed[x, z];

		var visited = new bool[source.Width * source.Height];
		var queue = new Queue<(int X, int Z)>();
		var component = new List<(int X, int Z)>();

		for (var z = 0; z < source.Height; z++)
		for (var x = 0; x < source.Width; x++)
		{
			var startIndex = z * source.Width + x;
			if (visited[startIndex] || suppressed[x, z] <= WaterComponentThreshold)
				continue;

			visited[startIndex] = true;
			queue.Enqueue((x, z));
			component.Clear();

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				component.Add(current);

				for (var oy = -1; oy <= 1; oy++)
				for (var ox = -1; ox <= 1; ox++)
				{
					if (ox == 0 && oy == 0)
						continue;

					var nx = current.X + ox;
					var nz = current.Z + oy;
					if (nx < 0 || nz < 0 || nx >= source.Width || nz >= source.Height)
						continue;

					var index = nz * source.Width + nx;
					if (visited[index] || suppressed[nx, nz] <= WaterComponentThreshold)
						continue;

					visited[index] = true;
					queue.Enqueue((nx, nz));
				}
			}

			if (component.Count >= minimumArea)
				continue;

			foreach (var (cx, cz) in component)
				result[cx, cz] = 0f;
		}

		return result;
	}

	private static Field SuppressWeakWaterNoise(Field source)
	{
		var result = Field.Rent(source.Width, source.Height);
		for (var z = 0; z < source.Height; z++)
		for (var x = 0; x < source.Width; x++)
		{
			var value = source[x, z];
			if (value <= WeakWaterThreshold)
			{
				result[x, z] = 0f;
				continue;
			}

			var sum = 0f;
			var count = 0;
			for (var oy = -1; oy <= 1; oy++)
			for (var ox = -1; ox <= 1; ox++)
			{
				var nx = Math.Clamp(x + ox, 0, source.Width - 1);
				var nz = Math.Clamp(z + oy, 0, source.Height - 1);
				sum += source[nx, nz];
				count++;
			}

			var neighborhood = sum / count;
			result[x, z] = neighborhood < 0.20f && value < 0.24f ? 0f : value;
		}

		return result;
	}

	private static RgbaBitmap ApplyWaterToSplatMap(RgbaBitmap source, Field elevation, Field waterMask, Field? oceanMask, float seaLevel)
	{
		var r = new Field(source.Width, source.Height);
		var g = new Field(source.Width, source.Height);
		var b = new Field(source.Width, source.Height);
		var a = new Field(source.Width, source.Height);

		for (var z = 0; z < source.Height; z++)
		for (var x = 0; x < source.Width; x++)
		{
			var water = Math.Clamp(waterMask[x, z], 0f, 1f);
			var ocean = oceanMask is null ? 0f : Math.Clamp(oceanMask[x, z], 0f, 1f);
			var inlandWater = Math.Max(0f, water - ocean);
			var coastal = oceanMask is null ? 0f : SampleCoastalInfluence(oceanMask, x, z);
			var wet = Math.Max(inlandWater * 0.9f, coastal * 0.4f);

			var grass = source.R[x, z] * (1f - wet * 0.78f);
			var dirt = Math.Max(source.G[x, z], 0.14f + wet * 0.52f);
			var rock = Math.Max(source.B[x, z], wet * 0.10f);
			var snow = source.A[x, z] * (1f - wet * 0.65f);

			if (ocean > 0.001f)
			{
				grass *= 1f - ocean;
				dirt = Math.Max(dirt, 0.60f + ocean * 0.30f);
				rock = Math.Max(rock * 0.40f, 0.10f + ocean * 0.12f);
				snow *= 1f - ocean;
			}
			else
			{
				var shoreBand = seaLevel > 0f
					? Math.Clamp(1f - ((elevation[x, z] - seaLevel) / Math.Max(0.08f, seaLevel * 0.35f)), 0f, 1f)
					: 0f;
				var beach = Math.Max(coastal * 0.8f, shoreBand * coastal);
				if (beach > 0.001f)
				{
					grass *= 1f - beach * 0.75f;
					dirt = Math.Max(dirt, 0.42f + beach * 0.32f);
					rock = Math.Max(rock, 0.06f + beach * 0.08f);
					snow *= 1f - beach * 0.65f;
				}
			}

			var sum = Math.Max(0.0001f, grass + dirt + rock + snow);
			r[x, z] = Math.Clamp(grass / sum, 0f, 1f);
			g[x, z] = Math.Clamp(dirt / sum, 0f, 1f);
			b[x, z] = Math.Clamp(rock / sum, 0f, 1f);
			a[x, z] = Math.Clamp(snow / sum, 0f, 1f);
		}

		return new RgbaBitmap(r, g, b, a);
	}

	private static RgbaBitmap BuildFallbackPaintBitmap(Field elevation, RgbaBitmap splatMap, Field waterMask)
	{
		var r = new Field(elevation.Width, elevation.Height);
		var g = new Field(elevation.Width, elevation.Height);
		var b = new Field(elevation.Width, elevation.Height);
		var a = new Field(elevation.Width, elevation.Height);

		var grass = new RgbaColor(0.35f, 0.39f, 0.28f, 1f);
		var dirt = new RgbaColor(0.56f, 0.48f, 0.36f, 1f);
		var rock = new RgbaColor(0.52f, 0.51f, 0.48f, 1f);
		var snow = new RgbaColor(0.80f, 0.81f, 0.78f, 1f);
		var mud = new RgbaColor(0.33f, 0.31f, 0.25f, 1f);

		for (var z = 0; z < elevation.Height; z++)
		for (var x = 0; x < elevation.Width; x++)
		{
			var color = BlendSplatColor(splatMap, x, z, grass, dirt, rock, snow);
			var wet = Math.Clamp(waterMask[x, z], 0f, 1f);
			color = LerpColor(color, mud, wet * 0.46f);
			r[x, z] = color.R;
			g[x, z] = color.G;
			b[x, z] = color.B;
			a[x, z] = 1f;
		}

		return new RgbaBitmap(r, g, b, a);
	}

	private static RgbaColor BlendSplatColor(RgbaBitmap splatMap, int x, int z, RgbaColor grass, RgbaColor dirt, RgbaColor rock, RgbaColor snow)
	{
		var wr = splatMap.R[x, z];
		var wg = splatMap.G[x, z];
		var wb = splatMap.B[x, z];
		var wa = splatMap.A[x, z];
		return new RgbaColor(
			grass.R * wr + dirt.R * wg + rock.R * wb + snow.R * wa,
			grass.G * wr + dirt.G * wg + rock.G * wb + snow.G * wa,
			grass.B * wr + dirt.B * wg + rock.B * wb + snow.B * wa,
			1f);
	}

	private static RgbaColor LerpColor(RgbaColor a, RgbaColor b, float t)
		=> new(
			a.R + (b.R - a.R) * t,
			a.G + (b.G - a.G) * t,
			a.B + (b.B - a.B) * t,
			1f);
}
