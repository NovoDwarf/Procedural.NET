namespace Procedural.NET.Core.Constants;

public static class PortKeys
{
	public const string A = "a";
	public const string B = "b";
	public const string Beach = "beach";
	public const string Bitmap = "bitmap";
	public const string Color = "color";
	public const string Cost = "cost";
	public const string G = "g";
	public const string C = "c";
	public const string Flow = "flow";
	public const string Mask = "mask";
	public const string Paths = "paths";
	public const string Points = "points";
	public const string Preview = "preview";
	public const string R = "r";
	public const string Slope = "slope";
	public const string Source = "source";
	public const string T = "t";
	public const string Uv = "uv";
	public const string Value = "value";
	
	public const string SplatBitmap = "splat_bitmap";
	public const string ScatterPrefix = "scatter_";
	public const string WaterPrefix = "water_";
	
	public static string Input(int index) => $"input_{index}";
	public static string Output(int index) => $"output_{index}";
	public static string Scatter(int index) => $"{ScatterPrefix}{index}";
	public static string Water(int index) => $"{WaterPrefix}{index}";
	
	public const string WaterDepth = "water_depth";
}
