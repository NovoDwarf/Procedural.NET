namespace Procedural.NET.Designer.Rendering;

internal sealed class PreviewFrame
{
	public const int Resolution = 512;

	public PreviewFrame(string cacheKey, byte[] bgraPixels, float[] heights)
	{
		CacheKey = cacheKey;
		BgraPixels = bgraPixels;
		HeightSamples = heights;
	}

	public string CacheKey { get; }
	public byte[] BgraPixels { get; }
	public float[] HeightSamples { get; }

	public float SampleHeight(float u, float v)
	{
		var x = ClampToPixel(u);
		var y = ClampToPixel(v);
		return HeightSamples[y * Resolution + x];
	}

	public PreviewColor SampleColor(float u, float v)
	{
		var x = ClampToPixel(u);
		var y = ClampToPixel(v);
		var offset = (y * Resolution + x) * 4;
		return new PreviewColor(
			BgraPixels[offset + 2],
			BgraPixels[offset + 1],
			BgraPixels[offset],
			BgraPixels[offset + 3]);
	}

	private static int ClampToPixel(float value)
	{
		return Math.Clamp((int)MathF.Round(value * (Resolution - 1)), 0, Resolution - 1);
	}
}
