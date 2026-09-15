
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Mathematics;

public sealed class StepNode : UnaryNode, ISampledExecutor, IFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 
	[
		new(ParameterKeys.Threshold, 0f, 1f, 0.001f, 0.5f, Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.Step;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Range;
	
	public override ColorF Color => new(0.36f, 0.42f, 0.58f);

	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	protected override float Transform(float value, Func<string, float>? param = null)
	{
		if (param == null)
			return float.NaN;

		var threshold = param(ParameterKeys.Threshold);
		
		return value >= threshold ? 1f : 0f;
	}

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var a = graph.RequireScalarInput(model, PortKeys.A, context);
		var threshold = graph.GetParameter(model, ParameterKeys.Threshold, context);
		return a >= threshold ? 1f : 0f;
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var aField = graph.RequireFieldInput(model, PortKeys.A, width, height);
		var result = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var threshold = graph.GetParameter(model, ParameterKeys.Threshold, (float)x / width, (float)y / height);
			result[x, y] = aField[x, y] >= threshold ? 1f : 0f;
		}
		return result;
	}
}
