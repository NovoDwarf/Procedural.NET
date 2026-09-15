namespace Procedural.NET.Designer.Rendering;

internal interface IPreviewRenderer
{
	string BackendName { get; }
	PreviewFrame Render(PreviewRenderRequest request);
}
