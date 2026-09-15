namespace Procedural.NET.Storage;

public sealed record SessionValidationResult(
	bool IsValid,
	IReadOnlyList<string> Errors,
	IReadOnlyList<string> Warnings)
{
	public static SessionValidationResult Valid(IReadOnlyList<string> warnings) => new(true, [], warnings);
	public static SessionValidationResult Invalid(IReadOnlyList<string> errors, IReadOnlyList<string>? warnings = null) => new(false, errors, warnings ?? []);
}
