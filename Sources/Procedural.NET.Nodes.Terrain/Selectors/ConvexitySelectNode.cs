
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Terrain.Selectors;

public sealed class ConvexitySelectNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	private static readonly IReadOnlyList<string> ConvexityOptions =
	[
		OptionLocalizationKeys.ConvexityTotal,
		OptionLocalizationKeys.ConvexityPlan,
		OptionLocalizationKeys.ConvexityProfile,
		OptionLocalizationKeys.ConvexityTangent
	];

	public override string Key => NodeKeys.ConvexitySelect;
	public override string GroupKey => NodeGroupKeys.Filters;
	public override string SubgroupKey => NodeSubgroupKeys.Selectors;
	
	public override ColorF Color => new(0.54f, 0.62f, 0.30f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.Source, ValueShape.Field, PortSemantics.Terrain)
	];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.Mask, ValueShape.Field, PortSemantics.Mask)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.ConvexityType, 0f, ConvexityOptions.Count - 1, 1f, 0f,
			UseSlider: false, Kind: ParameterKind.Option, Options: ConvexityOptions, Category: ParameterCategory.Primary),
		new(ParameterKeys.Scale, 0.1f, 16f, 0.1f, 4f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Min, 0f, 1f, 0.01f, 0.55f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Max, 0f, 1f, 0.01f, 1f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Falloff, 0f, 1f, 0.01f, 0.1f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.Invert, 0f, 1f, 1f, 0f,
			UseSlider: false, Kind: ParameterKind.Checkbox, Category: ParameterCategory.Primary)
	];

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context) => 0f;

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var source = graph.RequireFieldInput(model, PortKeys.Source, width, height);
		var convexityType = graph.GetParameter<int>(model, ParameterKeys.ConvexityType, 0.5f, 0.5f);
		var scale = Math.Max(0.1f, graph.GetParameter(model, ParameterKeys.Scale, 0.5f, 0.5f));
		var min = graph.GetParameter(model, ParameterKeys.Min, 0.5f, 0.5f);
		var max = graph.GetParameter(model, ParameterKeys.Max, 0.5f, 0.5f);
		
		if (min > max)
			(min, max) = (max, min);
		
		var falloff = MathF.Max(0f, graph.GetParameter(model, ParameterKeys.Falloff, 0.5f, 0.5f));
		var invert = graph.GetParameter<bool>(model, ParameterKeys.Invert, 0.5f, 0.5f);

		var result = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var normalized = SampleConvexity(source, x, y, width, height, convexityType, scale);
			var selected = SelectorMath.Range(normalized, min, max, falloff);
			result[x, y] = invert ? 1f - selected : selected;
		}

		return result;
	}

	private static float SampleConvexity(Field source, int x, int y, int width, int height, int type, float scale)
	{
		var xl = Math.Max(0, x - 1);
		var xr = Math.Min(width - 1, x + 1);
		var yu = Math.Max(0, y - 1);
		var yd = Math.Min(height - 1, y + 1);

		var c = source[x, y];
		var l = source[xl, y];
		var r = source[xr, y];
		var u = source[x, yu];
		var d = source[x, yd];
		var ul = source[xl, yu];
		var ur = source[xr, yu];
		var dl = source[xl, yd];
		var dr = source[xr, yd];

		var dx = (r - l) * 0.5f;
		var dy = (d - u) * 0.5f;
		var dxx = r - 2f * c + l;
		var dyy = d - 2f * c + u;
		var dxy = (dr - dl - ur + ul) * 0.25f;

		var gradient = MathF.Sqrt(dx * dx + dy * dy);
		var signed = type switch
		{
			1 => -(dxx + dyy),
			2 => -(dx * dx * dxx + 2f * dx * dy * dxy + dy * dy * dyy),
			3 => -(dxy - dx * dy),
			_ => -(dxx + dyy) - 0.5f * gradient
		};

		return 0.5f + MathF.Atan(signed * scale) / MathF.PI;
	}
}
