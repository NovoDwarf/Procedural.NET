using System.Text.Json;

namespace Procedural.NET.Storage.Macros;

public static class MacroParameterDefinitions
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public static IReadOnlyList<MacroParameterDefinition> Parse(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return [];

		var parsed = JsonSerializer.Deserialize<List<MacroParameterDefinition>>(raw, JsonOptions) ?? [];
		foreach (var definition in parsed)
		{
			if (string.IsNullOrWhiteSpace(definition.Id))
				definition.Id = Guid.NewGuid().ToString("N");
		}

		return parsed;
	}

	public static string Serialize(IEnumerable<MacroParameterDefinition> definitions) =>
		JsonSerializer.Serialize(definitions, JsonOptions);

	public static string PortKey(string definitionId) => $"macro_parameter_{definitionId}";
}