
using Avalonia.Media;
using NovoDwarf.Primitives.Models;

namespace Procedural.NET.Designer.Utilities;

public static class ColorUtils
{
	public static Color ToAvaloniaColor(ColorF color)
	{
		return Color.FromArgb(
			ToByte(color.A),
			ToByte(color.R),
			ToByte(color.G),
			ToByte(color.B));
	}

	public static byte ToByte(float value)
	{
		return (byte)(Math.Clamp(value, 0f, 1f) * byte.MaxValue);
	}

	public static Color BrushColor(IBrush brush)
	{
		return brush is ISolidColorBrush solid ? solid.Color : Colors.Gray;
	}
}