using Procedural.NET.Core.Constants;
using Procedural.NET.Designer.ViewModels;

namespace Procedural.NET.Designer.Rendering;

internal sealed class CpuPreviewRenderer : IPreviewRenderer
{
	private readonly Dictionary<PreviewCacheKey, PreviewFrame> _cache = [];

	public string BackendName => "CPU";

	public PreviewFrame Render(PreviewRenderRequest request)
	{
		var node = request.Node;
		var textureMode = request.TextureMode;
		var key = new PreviewCacheKey(node.Id, node.Model.Executor.Key, ParameterHash(node), textureMode);
		if (_cache.TryGetValue(key, out var cached))
			return cached;

		var seed = node.Model.Executor.Key.GetHashCode(StringComparison.Ordinal);
		var heights = new float[PreviewFrame.Resolution * PreviewFrame.Resolution];
		var pixels = new byte[PreviewFrame.Resolution * PreviewFrame.Resolution * 4];

		for (var y = 0; y < PreviewFrame.Resolution; y++)
		for (var x = 0; x < PreviewFrame.Resolution; x++)
		{
			var u = x / (float)(PreviewFrame.Resolution - 1);
			var v = y / (float)(PreviewFrame.Resolution - 1);
			var value = Sample(node, u, v, seed);
			heights[y * PreviewFrame.Resolution + x] = value;

			var color = TextureColor(node, value, textureMode);
			var offset = (y * PreviewFrame.Resolution + x) * 4;
			pixels[offset] = color.B;
			pixels[offset + 1] = color.G;
			pixels[offset + 2] = color.R;
			pixels[offset + 3] = color.A;
		}

		var frame = new PreviewFrame($"{key.NodeId:N}:{key.ExecutorKey}:{key.ParametersHash}:{key.TextureMode}", pixels, heights);
		_cache[key] = frame;
		return frame;
	}

	private static int ParameterHash(GraphNodeViewModel node)
	{
		var hash = new HashCode();
		foreach (var (key, value) in node.Parameters.OrderBy(static pair => pair.Key))
		{
			hash.Add(key, StringComparer.Ordinal);
			hash.Add(value);
		}

		return hash.ToHashCode();
	}

	private static float Sample(GraphNodeViewModel node, float u, float v, int seed)
	{
		if (node.Model.Executor.Key is NodeKeys.ConstantColor or NodeKeys.ColorRamp)
			return u;

		if (node.Model.Executor.Key.Contains("voronoi", StringComparison.OrdinalIgnoreCase))
			return Voronoi(u * 5f, v * 5f, seed);

		if (node.Model.Executor.Key.Contains("noise", StringComparison.OrdinalIgnoreCase))
			return ValueNoise(u * 8f, v * 8f, seed);

		if (node.Model.Executor.Key == NodeKeys.Gradient)
			return u;

		return Math.Clamp(0.5f + MathF.Sin((u * 5f + seed * 0.001f) * 2.4f) * MathF.Cos(v * 7f) * 0.35f, 0f, 1f);
	}

	private static float ValueNoise(float x, float y, int seed)
	{
		var x0 = (int)MathF.Floor(x);
		var y0 = (int)MathF.Floor(y);
		var tx = Smooth(x - x0);
		var ty = Smooth(y - y0);
		var a = Lerp(ValueAt(x0, y0, seed), ValueAt(x0 + 1, y0, seed), tx);
		var b = Lerp(ValueAt(x0, y0 + 1, seed), ValueAt(x0 + 1, y0 + 1, seed), tx);
		return Lerp(a, b, ty);
	}

	private static float Voronoi(float x, float y, int seed)
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

	private static float ValueAt(int x, int y, int seed)
	{
		var n = x * 374761393 + y * 668265263 + seed * 1442695041;
		n = (n ^ (n >> 13)) * 1274126177;
		return ((n ^ (n >> 16)) & 0x7fffffff) / (float)int.MaxValue;
	}

	private static float Smooth(float value) => value * value * (3f - 2f * value);
	private static float Lerp(float a, float b, float t) => a + (b - a) * t;
	private static byte ToByte(float value) => (byte)Math.Clamp(value, 0f, 255f);

	private static PreviewColor TextureColor(GraphNodeViewModel node, float height, string textureMode)
	{
		if (node.Model.Executor.Key == NodeKeys.ConstantColor)
			return new PreviewColor(
				ToByte(node.Model.Get(ParameterKeys.R) * 255),
				ToByte(node.Model.Get(ParameterKeys.G) * 255),
				ToByte(node.Model.Get(ParameterKeys.B) * 255),
				ToByte(node.Model.Get(ParameterKeys.A) * 255));

		if (node.Model.Executor.Key == NodeKeys.ColorRamp)
			return GradientColor(height);

		return textureMode switch
		{
			PreviewPalette.Gradient => GradientColor(height),
			PreviewPalette.Terrain => TerrainColor(height),
			PreviewPalette.Water => WaterColor(height),
			_ => GrayscaleColor(height)
		};
	}

	private static PreviewColor GrayscaleColor(float value)
	{
		var c = ToByte(value * 255);
		return new PreviewColor(c, c, c);
	}

	private static PreviewColor GradientColor(float value)
	{
		if (value < 0.5f)
			return Mix(new PreviewColor(28, 72, 164), new PreviewColor(80, 180, 124), value / 0.5f);

		return Mix(new PreviewColor(80, 180, 124), new PreviewColor(238, 220, 112), (value - 0.5f) / 0.5f);
	}

	private static PreviewColor TerrainColor(float height)
	{
		height = Math.Clamp(height, 0f, 1f);
		if (height < 0.28f)
			return Mix(new PreviewColor(29, 74, 113), new PreviewColor(46, 109, 148), height / 0.28f);
		if (height < 0.34f)
			return Mix(new PreviewColor(87, 115, 94), new PreviewColor(184, 166, 102), (height - 0.28f) / 0.06f);
		if (height < 0.62f)
			return Mix(new PreviewColor(71, 132, 72), new PreviewColor(112, 151, 88), (height - 0.34f) / 0.28f);
		if (height < 0.82f)
			return Mix(new PreviewColor(104, 101, 88), new PreviewColor(142, 136, 124), (height - 0.62f) / 0.20f);

		return Mix(new PreviewColor(186, 190, 185), new PreviewColor(241, 244, 238), (height - 0.82f) / 0.18f);
	}

	private static PreviewColor WaterColor(float height)
	{
		var t = Math.Clamp(height / 0.38f, 0f, 1f);
		return new PreviewColor(
			ToByte(18 + (42 - 18) * t),
			ToByte(70 + (122 - 70) * t),
			ToByte(112 + (154 - 112) * t),
			220);
	}

	private static PreviewColor Mix(PreviewColor a, PreviewColor b, float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return new PreviewColor(
			ToByte(a.R + (b.R - a.R) * t),
			ToByte(a.G + (b.G - a.G) * t),
			ToByte(a.B + (b.B - a.B) * t));
	}

	private readonly record struct PreviewCacheKey(Guid NodeId, string ExecutorKey, int ParametersHash, string TextureMode);
}
