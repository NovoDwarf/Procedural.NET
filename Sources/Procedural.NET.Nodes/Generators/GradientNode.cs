
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;
using Procedural.NET.Nodes.Enums;

namespace Procedural.NET.Nodes.Generators;

public sealed class GradientNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	public override string Key => NodeKeys.Gradient;
	public override string GroupKey => NodeGroupKeys.Generators;
	
	public override ColorF Color => new(0.24f, 0.50f, 0.44f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.Mask, ValueShape.Field),
		new(PortKeys.Uv, ValueShape.Float2)
	];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.C, ValueShape.Field, PortSemantics.Terrain)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Mode, 0f, 4f, 1f, 0f,
			UseSlider: false, Kind: ParameterKind.Option, Options: [
				OptionLocalizationKeys.GradientLinear, OptionLocalizationKeys.GradientRadial,
				OptionLocalizationKeys.GradientDiamond, OptionLocalizationKeys.GradientBox,
				OptionLocalizationKeys.GradientConstant
			], Category: ParameterCategory.Primary),

		new(ParameterKeys.Value, 0f, 1f, 0.01f, 0.5f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Angle, 0f, 360f, 1f, 0f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Offset, -1f, 1f, 0.01f, 0f),
		new(ParameterKeys.Contrast, 0.1f, 4f, 0.05f, 1f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Repeat, 1f, 16f, 1f, 1f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Invert, 0f, 1f, 1f, 0f, UseSlider: false, Kind: ParameterKind.Checkbox, Category: ParameterCategory.Primary)
	];

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var value = EvaluateScalar(model, context, graph);
		if (!graph.TryScalarInput(model, PortKeys.Mask, context, out var maskValue))
			return value;
		return Math.Clamp(value * maskValue, 0f, 1f);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var result = new Field(width, height);
		var hasMask = graph.TryFieldInput(model, PortKeys.Mask, width, height, out var maskBuffer);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var u = width <= 1 ? 0f : x / (float)(width - 1);
			var v = height <= 1 ? 0f : y / (float)(height - 1);
			var value = EvaluateScalar(model, new EvaluationContext(u, v), graph);
			result[x, y] = hasMask ? Math.Clamp(value * maskBuffer![x, y], 0f, 1f) : value;
		}

		return result;
	}
	
	private static float EvaluateScalar(GraphNode model, EvaluationContext context, IGraphExecutionContext graph)
	{
		var mode = graph.GetParameter<GradientMode>(model, ParameterKeys.Mode, context);

		if (mode == GradientMode.Constant)
			return Math.Clamp(graph.GetParameter(model, ParameterKeys.Value, context), 0f, 1f);

		var angle = graph.GetParameter(model, ParameterKeys.Angle, context) / 360f * (3.14f * 2f);
		var offset = graph.GetParameter(model, ParameterKeys.Offset, context);
		var contrast = MathF.Max(0.1f, graph.GetParameter(model, ParameterKeys.Contrast, context));
		var repeat = MathF.Max(1f, graph.GetParameter(model, ParameterKeys.Repeat, context));
		var centeredX = context.U - 0.5f;
		var centeredY = context.V - 0.5f;
		var value = mode switch
		{
			GradientMode.Radial => 1f - Math.Clamp(MathF.Sqrt(centeredX * centeredX + centeredY * centeredY) * MathF.Sqrt(2f), 0f, 1f),
			GradientMode.Diamond => 1f - Math.Clamp((MathF.Abs(centeredX) + MathF.Abs(centeredY)) * 2f, 0f, 1f),
			GradientMode.Box => 1f - Math.Clamp(MathF.Max(MathF.Abs(centeredX), MathF.Abs(centeredY)) * 2f, 0f, 1f),
			_ => 0.5f + centeredX * MathF.Cos(angle) + centeredY * MathF.Sin(angle)
		};

		value = repeat <= 1f ? value : value * repeat - MathF.Floor(value * repeat);
		value = 0.5f + (value - 0.5f) * contrast + offset;

		if (graph.GetParameter<bool>(model, ParameterKeys.Invert, context))
			value = 1f - value;

		return Math.Clamp(value, 0f, 1f);
	}
}
