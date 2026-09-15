
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Utilities;

public sealed class ChooserNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs = 	
	[
		new(PortKeys.A, ValueShape.Field),
		new(PortKeys.B, ValueShape.Field),
		new(PortKeys.Mask, ValueShape.Field, PortSemantics.Mask)
	];

	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs = 	
	[
		new(PortKeys.C, ValueShape.Field)
	];
	
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 	
	[
		new(ParameterKeys.Invert, 0f, 1f, 1f, 0f, 
			UseSlider: false, Kind: ParameterKind.Checkbox, Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.Chooser;
	public override string GroupKey => NodeGroupKeys.Utilities;
	
	public override ColorF Color => new(0.58f, 0.58f, 0.72f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var a = graph.RequireScalarInput(model, PortKeys.A, context);
		var b = graph.RequireScalarInput(model, PortKeys.B, context);
		var mask = Math.Clamp(graph.RequireScalarInput(model, PortKeys.Mask, context), 0f, 1f);
		
		if (graph.GetParameter<bool>(model, ParameterKeys.Invert, context))
			mask = 1f - mask;

		return a + (b - a) * mask;
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var a = graph.RequireFieldInput(model, PortKeys.A, width, height);
		var b = graph.RequireFieldInput(model, PortKeys.B, width, height);
		var mask = graph.RequireFieldInput(model, PortKeys.Mask, width, height);
		var invert = graph.GetParameter<bool>(model, ParameterKeys.Invert, 0.5f, 0.5f);
		var result = new Field(width, height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var t = Math.Clamp(mask[x, y], 0f, 1f);
			
			if (invert)
				t = 1f - t;
			
			result[x, y] = a[x, y] + (b[x, y] - a[x, y]) * t;
		}

		return result;
	}
}
