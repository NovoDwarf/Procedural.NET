namespace Procedural.NET.Nodes.Terrain.Selectors;

internal static class SelectorMath
{
	public static float Range(float value, float min, float max, float falloff)
	{
		if (falloff <= 0f)
			return value >= min && value <= max ? 1f : 0f;

		var enter = SmoothStep(min - falloff, min, value);
		var exit = 1f - SmoothStep(max, max + falloff, value);
		return Math.Clamp(enter * exit, 0f, 1f);
	}

	private static float SmoothStep(float edge0, float edge1, float value)
	{
		if (MathF.Abs(edge1 - edge0) < 0.00001f)
			return value >= edge1 ? 1f : 0f;

		var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0f, 1f);
		return t * t * (3f - 2f * t);
	}
}
