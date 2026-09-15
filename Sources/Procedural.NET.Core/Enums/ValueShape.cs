namespace Procedural.NET.Core.Enums;

public enum ValueShape
{
	// Scalar
	Float,
	Integer,
	Boolean,

	// Vector 2D
	Float2,
	Int2,

	// Vector 3D
	Float3,
	Int3,

	// Dataset
	Field,
	Volume,
	Points,
	Paths,

	// Wildcard
	Any,

	// Image
	Bitmap,
	Color
}
