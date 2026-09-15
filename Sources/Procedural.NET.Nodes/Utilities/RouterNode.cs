
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Utilities;

public sealed class RouterNode : BaseNode, IMultiSampledExecutor, IMultiFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs = 
	[
		new(PortKeys.C, ValueShape.Field)
	];

	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs = [];
	
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 	
	[
		new(ParameterKeys.OutputCount, 2f, 16f, 1f, 4f, UseSlider: false),
		new(ParameterKeys.SelectedOutput, 1f, 16f, 1f, 1f, UseSlider: false)
	];
	
	public override string Key => NodeKeys.Router;
	public override string GroupKey => NodeGroupKeys.Utilities;
	
	public override ColorF Color => new(0.30f, 0.34f, 0.48f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	public bool UpdatePorts(GraphNode model)
	{
		var count = Math.Clamp(model.Get<int>(ParameterKeys.OutputCount), 2, 16);

		return model.ReplaceOutputs(CreateOutputPorts(count));
	}

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, string outputKey, EvaluationContext context)
	{
		var selectedOutput = Math.Clamp(graph.GetParameter<int>(model, ParameterKeys.SelectedOutput, context), 1, Math.Max(1, model.Outputs.Count));

		return OutputIndex(outputKey) == selectedOutput
			? graph.RequireScalarInput(model, PortKeys.C, context)
			: 0f;
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height)
	{
		var selectedOutput = Math.Clamp(graph.GetParameter<int>(model, ParameterKeys.SelectedOutput, 0f, 0f), 1, Math.Max(1, model.Outputs.Count));

		return OutputIndex(outputKey) == selectedOutput
			? graph.RequireFieldInput(model, PortKeys.C, width, height)
			: new Field(width, height);
	}

	private static IEnumerable<GraphNodePort> CreateOutputPorts(int count)
	{
		for (var index = 1; index <= count; index++)
			yield return new GraphNodePort(PortKeys.Output(index), ValueShape.Field, PortSemantics.Generic, PortLocalizationKeys.Output(index));
	}

	private static int OutputIndex(string outputKey)
	{
		var separatorIndex = outputKey.LastIndexOf('_');

		if (separatorIndex < 0 || separatorIndex == outputKey.Length - 1)
			return 1;

		return int.TryParse(outputKey[(separatorIndex + 1)..], out var index)
			? index
			: 1;
	}
}
