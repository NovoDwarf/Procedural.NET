using Procedural.NET.Core.Enums;

namespace Procedural.NET.Storage.Macros;

/// <summary>
/// Maps the integer dropdown indices stored in <c>RuntimeShape</c> and <c>RuntimeSemantics</c>
/// parameters of MacroInput/MacroOutput nodes to their corresponding enum values.
/// Index order must match <c>MacroNodeContracts.RuntimeShapeOptions</c> and
/// <c>MacroNodeContracts.RuntimeSemanticsOptions</c>.
/// </summary>
public static class MacroRuntimeMapping
{
	public static ValueShape ShapeFromIndex(int index) => index switch
	{
		1 => ValueShape.Integer,
		2 => ValueShape.Boolean,
		3 => ValueShape.Field,
		4 => ValueShape.Bitmap,
		5 => ValueShape.Color,
		6 => ValueShape.Points,
		7 => ValueShape.Paths,
		_ => ValueShape.Float
	};

	public static PortSemantics SemanticsFromIndex(int index) => index switch
	{
		1 => PortSemantics.Terrain,
		2 => PortSemantics.Water,
		3 => PortSemantics.Mask,
		4 => PortSemantics.Texture,
		5 => PortSemantics.Slope,
		6 => PortSemantics.Curvature,
		7 => PortSemantics.Erosion,
		8 => PortSemantics.Density,
		9 => PortSemantics.Color,
		_ => PortSemantics.Generic
	};
}
