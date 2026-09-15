
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Filters;

public sealed class CurvesNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	public override string Key => NodeKeys.Curves;
	public override string GroupKey => NodeGroupKeys.Filters;
	
	public override ColorF Color => new(0.24f, 0.30f, 0.36f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.Source, ValueShape.Field),
		new(PortKeys.Mask, ValueShape.Field)
	];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.C, ValueShape.Field)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Midpoint, 0.01f, 0.99f, 0.01f, 0.5f,
			Category: ParameterCategory.Primary),

		new(ParameterKeys.Contrast, 0.05f, 4f, 0.05f, 1f,
			Category: ParameterCategory.Primary),

		new(ParameterKeys.Black, 0f, 1f, 0.01f, 0f,
			Category: ParameterCategory.Primary),

		new(ParameterKeys.White, 0f, 1f, 0.01f, 1f,
			Category: ParameterCategory.Primary)
	];

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var midpoint = Math.Clamp(graph.GetParameter(model, ParameterKeys.Midpoint, context), 0.01f, 0.99f);
		var contrast = MathF.Max(0.05f, graph.GetParameter(model, ParameterKeys.Contrast, context));
		var black = Math.Clamp(graph.GetParameter(model, ParameterKeys.Black, context), 0f, 1f);
		var white = Math.Clamp(graph.GetParameter(model, ParameterKeys.White, context), 0f, 1f);
		var input = Math.Clamp(graph.RequireScalarInput(model, PortKeys.Source, context), 0f, 1f);
		var value = EvaluateCurve(input, midpoint, contrast, black, white);
		
		if (!graph.TryScalarInput(model, PortKeys.Mask, context, out var blend))
			return value;
		
		return Math.Clamp(input * (1f - blend) + value * blend, 0f, 1f);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var source = graph.RequireFieldInput(model, PortKeys.Source, width, height);
		var hasMask = graph.TryFieldInput(model, PortKeys.Mask, width, height, out var maskBuffer);
		var result = new Field(width, height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var u = width <= 1 ? 0f : x / (float)(width - 1);
			var v = height <= 1 ? 0f : y / (float)(height - 1);
			var input = source[x, y];
			var value = EvaluateCurve(
				input,
				graph.GetParameter(model, ParameterKeys.Midpoint, u, v),
				graph.GetParameter(model, ParameterKeys.Contrast, u, v),
				graph.GetParameter(model, ParameterKeys.Black, u, v),
				graph.GetParameter(model, ParameterKeys.White, u, v));

			result[x, y] = hasMask
				? Math.Clamp(input * (1f - maskBuffer![x, y]) + value * maskBuffer[x, y], 0f, 1f)
				: value;
		}

		return result;
	}
	
	private static float EvaluateCurve(float input, float midpoint, float contrast, float black, float white)
	{
		midpoint = Math.Clamp(midpoint, 0.01f, 0.99f);
		contrast = MathF.Max(0.05f, contrast);
		black = Math.Clamp(black, 0f, 1f);
		white = Math.Clamp(white, 0f, 1f);

		var value = input < midpoint
			? 0.5f * MathF.Pow(input / midpoint, contrast)
			: 1f - 0.5f * MathF.Pow((1f - input) / (1f - midpoint), contrast);

		return Math.Clamp(black + value * (white - black), 0f, 1f);
	}
}
