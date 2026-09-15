using Procedural.NET.Designer.ViewModels;

namespace Procedural.NET.Designer.Rendering;

internal readonly record struct PreviewRenderRequest(
	GraphNodeViewModel Node,
	string TextureMode,
	bool IsThreeD = false,
	double Yaw = -35,
	double Pitch = 58,
	double Zoom = 1,
	bool HighResolution = false);
