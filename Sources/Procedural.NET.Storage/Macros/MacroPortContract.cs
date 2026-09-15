using Procedural.NET.Core.Enums;

namespace Procedural.NET.Storage.Macros;

public sealed class MacroPortContract
{
	public Guid NodeId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Shape { get; set; } = nameof(ValueShape.Float);
	public string Semantics { get; set; } = nameof(PortSemantics.Generic);
}
