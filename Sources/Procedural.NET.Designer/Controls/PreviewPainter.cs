using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Procedural.NET.Designer.Rendering;
using Procedural.NET.Designer.ViewModels;
using System.Runtime.InteropServices;

namespace Procedural.NET.Designer.Controls;

internal static class PreviewPainter
{
	private static readonly Dictionary<string, WriteableBitmap> BitmapCache = [];

	public const string PaletteGrayscale = PreviewPalette.Grayscale;
	public const string PaletteGradient = PreviewPalette.Gradient;
	public const string PaletteTerrain = PreviewPalette.Terrain;
	public const string PaletteWater = PreviewPalette.Water;

	public static void Draw(
		DrawingContext context,
		GraphNodeViewModel? node,
		Rect rect,
		bool prefer3D,
		double yaw = -35,
		double pitch = 58,
		double zoom = 1,
		string textureMode = PaletteGrayscale,
		bool highResolution = false)
	{
		context.DrawRectangle(new SolidColorBrush(Color.FromRgb(12, 14, 18)), new Pen(new SolidColorBrush(Color.FromRgb(52, 57, 67))), rect, 4);

		if (node is null || !node.HasPreview)
			return;

		var isThreeD = prefer3D || node.PreviewMode == "3D";
		var frame = PreviewRenderers.Current.Render(new PreviewRenderRequest(node, textureMode, isThreeD, yaw, pitch, zoom, highResolution));
		var viewport = Square(rect.Deflate(8));
		DrawTexture(context, frame, viewport);
	}

	private static void DrawTexture(DrawingContext context, PreviewFrame frame, Rect rect)
	{
		context.DrawImage(GetBitmap(frame), rect);
	}

	private static WriteableBitmap GetBitmap(PreviewFrame frame)
	{
		if (BitmapCache.TryGetValue(frame.CacheKey, out var cached))
			return cached;

		var bitmap = new WriteableBitmap(
			new PixelSize(PreviewFrame.Resolution, PreviewFrame.Resolution),
			new Vector(96, 96),
			PixelFormat.Bgra8888,
			AlphaFormat.Premul);

		using (var buffer = bitmap.Lock())
			Marshal.Copy(frame.BgraPixels, 0, buffer.Address, frame.BgraPixels.Length);

		BitmapCache[frame.CacheKey] = bitmap;
		return bitmap;
	}

	private static Rect Square(Rect rect)
	{
		var size = Math.Min(rect.Width, rect.Height);
		return new Rect(
			rect.X + (rect.Width - size) * 0.5,
			rect.Y + (rect.Height - size) * 0.5,
			size,
			size);
	}
}
