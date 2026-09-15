
using NovoDwarf.Primitives.Models;

using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Filters;

public sealed class ColorRampNode : BaseNode, IMultiBitmapExecutor
{
	public override string Key => NodeKeys.ColorRamp;
	public override string GroupKey => NodeGroupKeys.Filters;
	public override string SubgroupKey => NodeSubgroupKeys.Color;
	
	public override ColorF Color => new(0.72f, 0.48f, 0.72f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.Source, ValueShape.Field),
		new(PortKeys.A, ValueShape.Color, PortSemantics.Color),
		new(PortKeys.B, ValueShape.Color, PortSemantics.Color)
	];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.Bitmap, ValueShape.Bitmap, PortSemantics.Texture)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Invert, 0f, 1f, 1f, 0f,
			UseSlider: false, Kind: ParameterKind.Checkbox, Category: ParameterCategory.Primary)
	];

	public RgbaBitmap EvaluateBitmap(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height)
	{
		var source = graph.RequireFieldInput(model, PortKeys.Source, width, height);
		
		var colorA = graph.TryColorInput(model, PortKeys.A, new EvaluationContext(0.5f, 0.5f), out var a)
			? a!.Clamp()
			: new RgbaColor(0f, 0f, 0f, 1f);
		
		var colorB = graph.TryColorInput(model, PortKeys.B, new EvaluationContext(0.5f, 0.5f), out var b)
			? b!.Clamp()
			: RgbaColor.White;
		
		var invert = graph.GetParameter<bool>(model, ParameterKeys.Invert, 0.5f, 0.5f);

		var r = new Field(width, height);
		var g = new Field(width, height);
		var bField = new Field(width, height);
		var alpha = new Field(width, height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var t = Math.Clamp(source[x, y], 0f, 1f);
			if (invert)
				t = 1f - t;

			r[x, y] = Lerp(colorA.R, colorB.R, t);
			g[x, y] = Lerp(colorA.G, colorB.G, t);
			bField[x, y] = Lerp(colorA.B, colorB.B, t);
			alpha[x, y] = Lerp(colorA.A, colorB.A, t);
		}

		return new RgbaBitmap(r, g, bField, alpha);
	}

	private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
