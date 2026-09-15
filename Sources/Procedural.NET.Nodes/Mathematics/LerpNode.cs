
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Mathematics;

public sealed class LerpNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs = 	
	[
		new(PortKeys.A, ValueShape.Field),
		new(PortKeys.B, ValueShape.Field),
		new(PortKeys.T, ValueShape.Field)
	];

	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs =
	[
		new(PortKeys.C, ValueShape.Field)
	];
	
	public override string Key => NodeKeys.Lerp;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Range;
	
	public override ColorF Color => new(0.36f, 0.42f, 0.58f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var a = graph.RequireScalarInput(model, PortKeys.A, context);
		var b = graph.RequireScalarInput(model, PortKeys.B, context);
		var t = graph.RequireScalarInput(model, PortKeys.T, context);
		
		return a + (b - a) * Math.Clamp(t, 0f, 1f);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var aField = graph.RequireFieldInput(model, PortKeys.A, width, height);
		var bField = graph.RequireFieldInput(model, PortKeys.B, width, height);
		var tField = graph.RequireFieldInput(model, PortKeys.T, width, height);
		var result = Field.Rent(width, height);
		
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var tVal = Math.Clamp(tField[x, y], 0f, 1f);
			result[x, y] = aField[x, y] + (bField[x, y] - aField[x, y]) * tVal;
		}
		
		return result;
	}
}
