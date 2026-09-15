
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;
using Procedural.NET.Storage.Macros;

namespace Procedural.NET.Nodes.Macros;

public sealed class MacroParametersNode : BaseNode, IMultiSampledExecutor, IMultiFieldExecutor
{
	private const string DefaultPortName = "Parameter";

	public override string Key => NodeKeys.MacroParameters;
	public override string GroupKey => NodeGroupKeys.Utilities;
	public override string SubgroupKey => NodeSubgroupKeys.Macro;
	
	public override ColorF Color => new(0.52f, 0.40f, 0.22f);
	
	public override bool AllowParameterConnections => false;

	public override IReadOnlyList<GraphNodePort> Inputs => [];
	public override IReadOnlyList<GraphNodePort> Outputs => [];
	
	public bool UpdatePorts(GraphNode model)
	{
		var definitions = MacroParameterDefinitions.Parse(model.GetString(ParameterKeys.MacroParametersData));
		var outputs = definitions.Select(definition => new GraphNodePort(
				MacroNodeContracts.ParameterOutputPortKey(definition.Id),
				ParseShape(definition.Shape), PortSemantics.Generic, string.IsNullOrWhiteSpace(definition.Name) ? DefaultPortName : definition.Name)).ToArray();
		return model.ReplaceOutputs(outputs);
	}

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, string outputKey, EvaluationContext context)
	{
		if (MacroEvaluationScope.TryGetValue(MacroNodeContracts.ParameterBindingKey(model.Id, outputKey), out var scoped))
			return MacroNodeContracts.ReadScalar(scoped, context);

		var definition = FindDefinition(model, outputKey);
		var value = definition?.DefaultValue ?? 0f;
		
		return ParseShape(definition?.Shape) switch
		{
			ValueShape.Boolean => value >= 0.5f ? 1f : 0f,
			ValueShape.Integer => MathF.Round(value),
			_ => value
		};
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height)
	{
		if (MacroEvaluationScope.TryGetValue(MacroNodeContracts.ParameterBindingKey(model.Id, outputKey), out var scoped))
			return MacroNodeContracts.ReadField(scoped, width, height);

		var value = EvaluatePoint(model, graph, outputKey, new EvaluationContext(0.5f, 0.5f));
		var result = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			result[x, y] = value;
		return result;
	}

	private static IReadOnlyList<MacroParameterDefinition> GetDefinitions(GraphNode model)
	{
		return MacroParameterDefinitions.Parse(model.GetString(ParameterKeys.MacroParametersData));
	}

	private static MacroParameterDefinition? FindDefinition(GraphNode model, string outputKey)
	{
		var definitions = GetDefinitions(model);
		if (definitions.Count == 0)
			return null;

		return definitions.FirstOrDefault(definition =>
			string.Equals(MacroNodeContracts.ParameterOutputPortKey(definition.Id), outputKey, StringComparison.Ordinal));
	}

	private static ValueShape ParseShape(string? shape) =>
		Enum.TryParse<ValueShape>(shape, ignoreCase: true, out var parsed) ? parsed : ValueShape.Float;
}
