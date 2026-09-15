using Procedural.NET.Core.Enums;

namespace Procedural.NET.Core;

public record struct GraphNodePort(
	string Key,
	ValueShape Shape,
	PortSemantics Semantics = PortSemantics.Generic,
	string? CustomLocKey = null);
