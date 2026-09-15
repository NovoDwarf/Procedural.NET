
using NovoDwarf.Primitives.Models;

using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Blending;

public sealed class BitmapBlendNode : BaseNode, IMultiBitmapExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs =
	[
		new(PortKeys.A, ValueShape.Bitmap, PortSemantics.Texture),
		new(PortKeys.B, ValueShape.Bitmap, PortSemantics.Texture),
		new(PortKeys.Mask, ValueShape.Field, PortSemantics.Mask)
	];

	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs =
	[
		new(PortKeys.Bitmap, ValueShape.Bitmap, PortSemantics.Texture)
	];

	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters =
	[
		new(ParameterKeys.Invert, 0f, 1f, 1f, 0f,
			UseSlider: false, Kind: ParameterKind.Checkbox, Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.BitmapBlend;
	public override string GroupKey => NodeGroupKeys.Blending;
	public override string SubgroupKey => NodeSubgroupKeys.Color;
	
	public override ColorF Color => new(0.62f, 0.64f, 0.86f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	public RgbaBitmap EvaluateBitmap(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height)
	{
		var a = graph.RequireBitmapInput(model, PortKeys.A, width, height);
		var b = graph.RequireBitmapInput(model, PortKeys.B, width, height);
		var mask = graph.RequireFieldInput(model, PortKeys.Mask, width, height);
		var invert = graph.GetParameter<bool>(model, ParameterKeys.Invert, 0.5f, 0.5f);

		return new RgbaBitmap(
			BlendChannel(a.R, b.R, mask, invert, width, height),
			BlendChannel(a.G, b.G, mask, invert, width, height),
			BlendChannel(a.B, b.B, mask, invert, width, height),
			BlendChannel(a.A, b.A, mask, invert, width, height));
	}

	private static Field BlendChannel(Field a, Field b, Field mask, bool invert, int width, int height)
	{
		var result = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var t = Math.Clamp(mask[x, y], 0f, 1f);
			if (invert)
				t = 1f - t;
			result[x, y] = a[x, y] + (b[x, y] - a[x, y]) * t;
		}

		return result;
	}
}
