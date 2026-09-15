
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes;

public abstract class BinaryNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs =
	[
		new(PortKeys.A, ValueShape.Field),
		new(PortKeys.B, ValueShape.Field)
	];

	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs =
	[
		new(PortKeys.C, ValueShape.Field)
	];
	
	public override ColorF Color => new(0.36f, 0.42f, 0.58f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;

	protected abstract float Combine(float a, float b);
	
	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var a = graph.RequireScalarInput(model, PortKeys.A, context);
		var b = graph.RequireScalarInput(model, PortKeys.B, context);
		
		return Combine(a, b);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var aField = graph.RequireFieldInput(model, PortKeys.A, width, height);
		var bField = graph.RequireFieldInput(model, PortKeys.B, width, height);
		var result = new Field(width, height);
		
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			result[x, y] = Combine(aField[x, y], bField[x, y]);
		
		return result;
	}
}
