
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Terrain.Selectors;

public sealed class WetnessSelectNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	public override string Key => NodeKeys.WetnessSelect;
	public override string GroupKey => NodeGroupKeys.Filters;
	public override string SubgroupKey => NodeSubgroupKeys.Selectors;
	
	public override ColorF Color => new(0.24f, 0.48f, 0.68f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.Mask, ValueShape.Field, PortSemantics.Water)
	];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.Mask, ValueShape.Field, PortSemantics.Mask)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Min, 0f, 1f, 0.01f, 0.1f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Max, 0f, 1f, 0.01f, 0.6f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Falloff, 0f, 1f, 0.01f, 0.1f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Invert, 0f, 1f, 1f, 0f,
			UseSlider: false, Kind: ParameterKind.Checkbox, Category: ParameterCategory.Primary)
	];

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var value = graph.RequireScalarInput(model, PortKeys.Mask, context);
		return Select(model, graph, value, context);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var source = graph.RequireFieldInput(model, PortKeys.Mask, width, height);
		var result = new Field(width, height);
		
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var u = width <= 1 ? 0f : x / (float)(width - 1);
			var v = height <= 1 ? 0f : y / (float)(height - 1);
			result[x, y] = Select(model, graph, source[x, y], new EvaluationContext(u, v));
		}

		return result;
	}

	private static float Select(GraphNode model, IGraphExecutionContext graph, float value, EvaluationContext context)
	{
		var min = graph.GetParameter(model, ParameterKeys.Min, context);
		var max = graph.GetParameter(model, ParameterKeys.Max, context);
		
		if (min > max)
			(min, max) = (max, min);

		var falloff = MathF.Max(0f, graph.GetParameter(model, ParameterKeys.Falloff, context));
		var selected = SelectorMath.Range(value, min, max, falloff);
		return graph.GetParameter<bool>(model, ParameterKeys.Invert, context) ? 1f - selected : selected;
	}
}
