using Procedural.NET.Core.Enums;

namespace Procedural.NET.Storage.Macros;

public sealed class MacroParameterDefinition
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public string Name { get; set; } = "Parameter";
	public string Shape { get; set; } = nameof(ValueShape.Float);
	public float DefaultValue { get; set; } = 0.5f;
	public float Min { get; set; }
	public float Max { get; set; } = 1f;
	public float Step { get; set; } = 0.01f;
}