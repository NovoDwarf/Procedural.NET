
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Utilities;

public sealed class SwitchNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs = [];

	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs = 
	[
		new(PortKeys.C, ValueShape.Field)
	];
	
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 	
	[
		new(ParameterKeys.InputCount, 2f, 16f, 1f, 4f, UseSlider: false),

		new(ParameterKeys.SelectedInput, 1f, 16f, 1f, 1f, UseSlider: false)
	];
	
	public override string Key => NodeKeys.Switch;
	public override string GroupKey => NodeGroupKeys.Utilities;
	
	public override ColorF Color => new(0.30f, 0.34f, 0.48f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	public bool UpdatePorts(GraphNode model)
	{
		var count = Math.Clamp(model.Get<int>(ParameterKeys.InputCount), 2, 16);
		
		return model.ReplaceInputs(CreateInputPorts(count));
	}

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var inputCount = Math.Max(1, model.Inputs.Count);
		var selectedInput = Math.Clamp(graph.GetParameter<int>(model, ParameterKeys.SelectedInput, context), 1, inputCount);
		
		return graph.RequireScalarInput(model, PortKeys.Input(selectedInput), context);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var inputCount = Math.Max(1, model.Inputs.Count);
		var selectedInput = Math.Clamp(graph.GetParameter<int>(model, ParameterKeys.SelectedInput, 0f, 0f), 1, inputCount);
		
		return graph.RequireFieldInput(model, PortKeys.Input(selectedInput), width, height);
	}

	private static IEnumerable<GraphNodePort> CreateInputPorts(int count)
	{
		for (var index = 1; index <= count; index++)
			yield return new GraphNodePort(PortKeys.Input(index), ValueShape.Field, PortSemantics.Generic, PortLocalizationKeys.Input(index));
	}
}
