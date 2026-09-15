
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;
using Procedural.NET.Nodes.Enums;

namespace Procedural.NET.Nodes.Filters;

public sealed class EqualizerNode : UnaryNode, ISampledExecutor, IFieldExecutor
{
	private const int BinCount = 256;

	public override string Key => NodeKeys.Equalizer;
	public override string GroupKey => NodeGroupKeys.Filters;
	
	public override ColorF Color => new(0.36f, 0.50f, 0.44f);
	
	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Strength, 0f, 1f, 0.01f, 1f,
			Category: ParameterCategory.Primary),

		new(ParameterKeys.EqualizerProfile, 0f, 3f, 1f, 0f,
			UseSlider: false,
			Kind: ParameterKind.Option,
			Options: [
				OptionLocalizationKeys.EqualizerUniform,
				OptionLocalizationKeys.EqualizerExponential,
				OptionLocalizationKeys.EqualizerLogNormal,
				OptionLocalizationKeys.EqualizerGaussian
			],
			Category: ParameterCategory.Primary),

		new(ParameterKeys.NormalizeOutput, 0f, 1f, 1f, 1f,
			UseSlider: false, Kind: ParameterKind.Checkbox, Category: ParameterCategory.Primary)
	];

	protected override float Transform(float value, Func<string, float>? param = null) => value;

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context) =>
		graph.RequireScalarInput(model, PortKeys.A, context);

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var source = graph.RequireFieldInput(model, PortKeys.A, width, height);
		var strength = Math.Clamp(graph.GetParameter(model, ParameterKeys.Strength, 0.5f, 0.5f), 0f, 1f);
		var profile = graph.GetParameter<EqualizerProfile>(model, ParameterKeys.EqualizerProfile, 0.5f, 0.5f);
		var normalize = graph.GetParameter<bool>(model, ParameterKeys.NormalizeOutput, 0.5f, 0.5f);

		if (strength <= 0.0001f)
			return CloneField(source, width, height);

		var cdf = BuildCdf(source, width, height);
		var result = new Field(width, height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var original = Math.Clamp(source[x, y], 0f, 1f);
			var equalized = EvaluateEqualized(original, cdf, profile);
			result[x, y] = Math.Clamp(original + (equalized - original) * strength, 0f, 1f);
		}

		return normalize ? Normalize(result, width, height) : result;
	}

	private static float[] BuildCdf(Field source, int width, int height)
	{
		var histogram = new int[BinCount];
		var total = Math.Max(1, width * height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var bin = (int)MathF.Round(Math.Clamp(source[x, y], 0f, 1f) * (BinCount - 1));
			histogram[Math.Clamp(bin, 0, BinCount - 1)]++;
		}

		var cdf = new float[BinCount];
		var cumulative = 0;
		
		for (var i = 0; i < BinCount; i++)
		{
			cumulative += histogram[i];
			cdf[i] = cumulative / (float)total;
		}

		return cdf;
	}

	private static float EvaluateEqualized(float value, IReadOnlyList<float> cdf, EqualizerProfile profile)
	{
		var scaled = value * (BinCount - 1);
		var low = Math.Clamp((int)MathF.Floor(scaled), 0, BinCount - 1);
		var high = Math.Clamp(low + 1, 0, BinCount - 1);
		var t = scaled - low;
		var uniform = cdf[low] + (cdf[high] - cdf[low]) * t;
		return Math.Clamp(ApplyProfile(uniform, profile), 0f, 1f);
	}

	private static float ApplyProfile(float value, EqualizerProfile profile)
	{
		const float curve = 4f;
		
		return profile switch
		{
			EqualizerProfile.Exponential => (MathF.Exp(value * curve) - 1f) / (MathF.Exp(curve) - 1f),
			EqualizerProfile.LogNormal => MathF.Log(1f + value * curve) / MathF.Log(1f + curve),
			EqualizerProfile.Gaussian => value * value * (3f - 2f * value),
			_ => value
		};
	}

	private static Field Normalize(Field source, int width, int height)
	{
		var min = float.MaxValue;
		var max = float.MinValue;

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var value = source[x, y];
			if (value < min) min = value;
			if (value > max) max = value;
		}

		if (max - min <= 0.0001f)
			return CloneField(source, width, height);

		var scale = 1f / (max - min);
		var result = new Field(width, height);
		
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			result[x, y] = Math.Clamp((source[x, y] - min) * scale, 0f, 1f);

		return result;
	}

	private static Field CloneField(Field source, int width, int height)
	{
		var result = new Field(width, height);
		
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			result[x, y] = source[x, y];

		return result;
	}
}
