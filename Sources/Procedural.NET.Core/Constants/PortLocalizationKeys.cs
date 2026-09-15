namespace Procedural.NET.Core.Constants;

public static class PortLocalizationKeys
{
	public static string Input(int index)   => $"generator.port.input|{index}";
	public static string Output(int index)  => $"generator.port.output|{index}";
	public static string Scatter(int index) => $"generator.port.scatter|{index}";
	public static string Water(int index)   => $"generator.port.water|{index}";
}
