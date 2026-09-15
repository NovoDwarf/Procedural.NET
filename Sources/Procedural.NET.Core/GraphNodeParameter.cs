using Procedural.NET.Core.Enums;

namespace Procedural.NET.Core;

public sealed record GraphNodeParameter(
	string Key,
	float Min,
	float Max,
	float Step,
	float DefaultValue,
	string Section = "generator.designer.parameters",
	string Suffix = "",
	bool UseSlider = true,
	ParameterKind Kind = ParameterKind.Number,
	IReadOnlyList<string>? Options = null,
	bool AllowGreater = false,
	bool AllowLesser = false,
	ParameterCategory Category = ParameterCategory.Advanced,
	string? CustomLocKey = null);
