namespace Procedural.NET.Core.Enums;

public enum PortSemantics
{
	Generic,      // no restriction – wildcard

	// Field semantics
	Terrain,   // elevation / heightmap
	Water,     // water depth, flow, rivers, lakes
	Mask,      // binary or float mask (selection, weight)
	Texture,   // colour / albedo (single-channel)
	Slope,     // gradient / steepness
	Curvature, // surface curvature (concave ↔ convex)
	Erosion,   // erosion amount / sediment
	Density,   // coverage / vegetation density

	// Vector semantics
	Position,  // world-space position (Float2 / Float3)
	Direction, // normalized direction (Float2 / Float3)
	Normal,    // surface normal (Float3)
	UV,        // texture coordinates (Float2)
	Color,     // RGB colour (Float3)
}
