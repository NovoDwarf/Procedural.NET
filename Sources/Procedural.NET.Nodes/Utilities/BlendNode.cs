
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Utilities;

public sealed class BlendNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs = 	
	[
		new(PortKeys.A, ValueShape.Field),
		new(PortKeys.B, ValueShape.Field),
		new(PortKeys.Mask, ValueShape.Field)
	];

	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs = 	
	[
		new(PortKeys.C, ValueShape.Field)
	];
	
	public override string Key => NodeKeys.Blend;
	public override string GroupKey => NodeGroupKeys.Utilities;

	public override ColorF Color => new(0.30f, 0.34f, 0.48f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	
	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var a = graph.RequireScalarInput(model, PortKeys.A, context);
		var b = graph.RequireScalarInput(model, PortKeys.B, context);
		var mask = graph.RequireScalarInput(model, PortKeys.Mask, context);
		
		return a + (b - a) * Math.Clamp(mask, 0f, 1f);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var aField = graph.RequireFieldInput(model, PortKeys.A, width, height);
		var bField = graph.RequireFieldInput(model, PortKeys.B, width, height);
		var maskField = graph.RequireFieldInput(model, PortKeys.Mask, width, height);
		
		var result = new Field(width, height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var t = Math.Clamp(maskField[x, y], 0f, 1f);
			result[x, y] = aField[x, y] + (bField[x, y] - aField[x, y]) * t;
		}

		return result;
	}
}
