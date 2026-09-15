using System.Text.Json;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;

namespace Procedural.NET.Storage.Macros;

/// <summary>
/// Writes macro-specific metadata tags into a captured <see cref="GraphSession"/>.
/// Called by <see cref="GraphSessionService.CaptureSelection"/> after the graphNode list is built.
/// </summary>
internal static class MacroSessionMetadataBuilder
{
	public static void Apply(GraphSession session)
	{
		session.Metadata.Tags["macro.kind"] = "graph";
		session.Metadata.Tags["macro.inputs"] = JsonSerializer.Serialize(session.Nodes
			.Where(node => string.Equals(node.ExecutorKey, NodeKeys.MacroInput, StringComparison.Ordinal))
			.OrderBy(static node => node.PositionY)
			.ThenBy(static node => node.PositionX)
			.Select(BuildInputContract));
		session.Metadata.Tags["macro.outputs"] = JsonSerializer.Serialize(session.Nodes
			.Where(node => string.Equals(node.ExecutorKey, NodeKeys.MacroOutput, StringComparison.Ordinal))
			.OrderBy(static node => node.PositionY)
			.ThenBy(static node => node.PositionX)
			.Select(BuildOutputContract));
		session.Metadata.Tags["macro.views"] = JsonSerializer.Serialize(session.Nodes
			.Where(node => string.Equals(node.ExecutorKey, NodeKeys.MacroView, StringComparison.Ordinal))
			.OrderBy(static node => node.PositionY)
			.ThenBy(static node => node.PositionX)
			.Select(BuildOutputContract));
		session.Metadata.Tags["macro.parameters"] = JsonSerializer.Serialize(BuildParameterContracts(session));
	}

	private static MacroPortContract BuildInputContract(SessionNode node) => new()
	{
		NodeId = node.Id,
		Name = GetString(node, ParameterKeys.DisplayName, "Input"),
		Shape = ResolveShape(node).ToString(),
		Semantics = ResolveSemantics(node).ToString()
	};

	private static MacroPortContract BuildOutputContract(SessionNode node) => new()
	{
		NodeId = node.Id,
		Name = GetString(node, ParameterKeys.DisplayName, "Output"),
		Shape = ResolveShape(node).ToString(),
		Semantics = ResolveSemantics(node).ToString()
	};

	private static IReadOnlyList<MacroParameterContract> BuildParameterContracts(GraphSession session)
	{
		var contracts = new List<MacroParameterContract>();
		foreach (var node in session.Nodes
			         .Where(node => string.Equals(node.ExecutorKey, NodeKeys.MacroParameters, StringComparison.Ordinal))
			         .OrderBy(static node => node.PositionY)
			         .ThenBy(static node => node.PositionX))
		{
			var definitions = MacroParameterDefinitions.Parse(
				node.StringParameters.TryGetValue(ParameterKeys.MacroParametersData, out var raw) ? raw : string.Empty);

			foreach (var definition in definitions)
			{
				contracts.Add(new MacroParameterContract
				{
					NodeId = node.Id,
					PortKey = MacroParameterDefinitions.PortKey(definition.Id),
					Name = string.IsNullOrWhiteSpace(definition.Name) ? "Parameter" : definition.Name,
					Shape = definition.Shape,
					DefaultValue = definition.DefaultValue,
					Min = definition.Min,
					Max = definition.Max,
					Step = definition.Step
				});
			}
		}

		return contracts;
	}

	private static ValueShape ResolveShape(SessionNode node) =>
		MacroRuntimeMapping.ShapeFromIndex(GetInt(node, ParameterKeys.RuntimeShape));

	private static PortSemantics ResolveSemantics(SessionNode node) =>
		MacroRuntimeMapping.SemanticsFromIndex(GetInt(node, ParameterKeys.RuntimeSemantics));

	private static string GetString(SessionNode node, string key, string fallback) =>
		node.StringParameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
			? value
			: fallback;

	private static int GetInt(SessionNode node, string key) => (int)(node.Parameters.GetValueOrDefault(key, 0f));
}
