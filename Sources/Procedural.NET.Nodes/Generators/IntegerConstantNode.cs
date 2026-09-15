
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Generators;

public sealed class IntegerConstantNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	public override string Key => NodeKeys.ConstantInteger;
	public override string GroupKey => NodeGroupKeys.Generators;
	public override string SubgroupKey => NodeSubgroupKeys.Constants;
	
	public override ColorF Color => new(0.24f, 0.30f, 0.36f);
	
	public override bool AllowParameterConnections => false;

	public override IReadOnlyList<GraphNodePort> Inputs => [];
	
	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.Value, ValueShape.Integer)
	];
	
	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Value, -99999f, 99999f, 1f, 0f, AllowGreater: true, AllowLesser: true, Category: ParameterCategory.Primary)
	];

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
		=> MathF.Round(graph.GetParameter(model, ParameterKeys.Value, context));

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var value = MathF.Round(graph.GetParameter(model, ParameterKeys.Value, 0.5f, 0.5f));
		var result = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			result[x, y] = value;
		return result;
	}
}
