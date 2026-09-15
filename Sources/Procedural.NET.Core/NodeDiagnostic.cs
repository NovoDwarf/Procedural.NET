namespace Procedural.NET.Core;

public enum DiagnosticSeverity
{
	Info,
	Warning,
	Error
}

public sealed record NodeDiagnostic(
	Guid NodeId,
	DiagnosticSeverity Severity,
	string Message,
	string? ParameterKey = null);
