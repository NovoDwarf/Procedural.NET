
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Blending;

public sealed class CombinerNode : BaseNode, ISampledExecutor, IFieldExecutor
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

	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters =
	[
		new(ParameterKeys.Method, 0f, MethodOptions.Count - 1, 1f, 0f,
			UseSlider: false, Kind: ParameterKind.Option, Options: MethodOptions, Category: ParameterCategory.Primary),
		new(ParameterKeys.Strength, 0f, 1f, 0.01f, 1f, Category: ParameterCategory.Primary)
	];
	
	private static readonly IReadOnlyList<string> MethodOptions =
	[
		OptionLocalizationKeys.CombinerAverage,
		OptionLocalizationKeys.CombinerAdd,
		OptionLocalizationKeys.CombinerSubtract,
		OptionLocalizationKeys.CombinerMultiply,
		OptionLocalizationKeys.CombinerDivide,
		OptionLocalizationKeys.CombinerScreen,
		OptionLocalizationKeys.CombinerOverlay,
		OptionLocalizationKeys.CombinerMax,
		OptionLocalizationKeys.CombinerMin,
		OptionLocalizationKeys.CombinerPower,
		OptionLocalizationKeys.CombinerRoot,
		OptionLocalizationKeys.CombinerExtractDifferences,
		OptionLocalizationKeys.CombinerDetailWithDifferences,
		OptionLocalizationKeys.CombinerAbsoluteDifferences
	];

	public override string Key => NodeKeys.Combiner;
	public override string GroupKey => NodeGroupKeys.Blending;
	
	public override ColorF Color => new(0.38f, 0.40f, 0.58f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var a = graph.RequireScalarInput(model, PortKeys.A, context);
		var b = graph.RequireScalarInput(model, PortKeys.B, context);
		var method = graph.GetParameter<int>(model, ParameterKeys.Method, context);
		var strength = Math.Clamp(graph.GetParameter(model, ParameterKeys.Strength, context), 0f, 1f);
		var combined = Combine(a, b, method);
		return BlendWithStrength(a, combined, strength);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var a = graph.RequireFieldInput(model, PortKeys.A, width, height);
		var b = graph.RequireFieldInput(model, PortKeys.B, width, height);
		var method = graph.GetParameter<int>(model, ParameterKeys.Method, 0.5f, 0.5f);
		var strength = Math.Clamp(graph.GetParameter(model, ParameterKeys.Strength, 0.5f, 0.5f), 0f, 1f);
		var result = new Field(width, height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var combined = Combine(a[x, y], b[x, y], method);
			result[x, y] = BlendWithStrength(a[x, y], combined, strength);
		}

		return result;
	}

	private static float BlendWithStrength(float original, float combined, float strength) =>
		Math.Clamp(original + (combined - original) * strength, 0f, 1f);

	private static float Combine(float a, float b, int method)
	{
		a = Math.Clamp(a, 0f, 1f);
		b = Math.Clamp(b, 0f, 1f);

		return method switch
		{
			0 => (a + b) * 0.5f,
			1 => a + b,
			2 => a - b,
			3 => a * b,
			4 => b == 0f ? 1f : a / b,
			5 => 1f - (1f - a) * (1f - b),
			6 => a < 0.5f ? 2f * a * b : 1f - 2f * (1f - a) * (1f - b),
			7 => Math.Max(a, b),
			8 => Math.Min(a, b),
			9 => MathF.Pow(Math.Max(a, 0.0001f), Math.Clamp(b, 0.0001f, 8f)),
			10 => MathF.Pow(Math.Max(a, 0.0001f), 1f / Math.Clamp(b, 0.0001f, 8f)),
			11 => a - b,
			12 => a + (a - b),
			13 => Math.Abs(a - b),
			_ => a
		};
	}
}
