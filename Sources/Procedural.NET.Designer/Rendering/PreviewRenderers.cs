namespace Procedural.NET.Designer.Rendering;

internal static class PreviewRenderers
{
	public static IPreviewRenderer Current { get; } = new SilkPreviewRenderer(new CpuPreviewRenderer());
}
