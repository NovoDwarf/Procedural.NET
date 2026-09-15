namespace Procedural.NET.Storage.Macros;

public sealed class MacroDefinition
{
	public string Reference { get; init; } = string.Empty;
	public string SourcePath { get; init; } = string.Empty;
	
	public GraphSession Session { get; init; } = new();
	
	public IReadOnlyList<MacroPortContract> Inputs { get; init; } = [];
	public IReadOnlyList<MacroPortContract> Outputs { get; init; } = [];
	public IReadOnlyList<MacroPortContract> Views { get; init; } = [];
	public IReadOnlyList<MacroParameterContract> Parameters { get; init; } = [];
}