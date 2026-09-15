using Procedural.NET.Core.Enums;

namespace Procedural.NET.Storage.Macros;

public sealed class MacroParameterContract
{
	public Guid NodeId { get; set; }
	
	public string PortKey { get; set; } = "value";
	public string Name { get; set; } = string.Empty;
	public string Shape { get; set; } = nameof(ValueShape.Float);
	
	public float DefaultValue { get; set; }
	
	public float Min { get; set; }
	public float Max { get; set; } = 1f;
	public float Step { get; set; } = 0.01f;
}
