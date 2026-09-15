
using NovoDwarf.Primitives.Extensions;
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Generators;

public sealed class BitmapNode : BaseNode, IMultiBitmapExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs = 	
	[
		new(PortKeys.Color, ValueShape.Color, PortSemantics.Color),
		new(PortKeys.R, ValueShape.Field),
		new(PortKeys.G, ValueShape.Field),
		new(PortKeys.B, ValueShape.Field),
		new(PortKeys.A, ValueShape.Field)
	];

	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs =
	[
		new(PortKeys.Bitmap, ValueShape.Bitmap, PortSemantics.Texture)
	];
	
	public override string Key => NodeKeys.Bitmap;
	public override string GroupKey => NodeGroupKeys.Generators;
	public override string SubgroupKey => NodeSubgroupKeys.Constants;
	
	public override ColorF Color => new(0.42f, 0.34f, 0.54f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	
	public RgbaBitmap EvaluateBitmap(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height)
	{
		var baseColor = graph.TryColorInput(model, PortKeys.Color, new EvaluationContext(0.5f, 0.5f), out var color)
			? color!
			: RgbaColor.White;
		
		var r = graph.TryFieldInput(model, PortKeys.R, width, height, out var red)
			? red!
			: FieldExtensions.Fill(width, height, baseColor.R);
		var g = graph.TryFieldInput(model, PortKeys.G, width, height, out var green)
			? green!
			: FieldExtensions.Fill(width, height, baseColor.G);
		var b = graph.TryFieldInput(model, PortKeys.B, width, height, out var blue)
			? blue!
			: FieldExtensions.Fill(width, height, baseColor.B);
		var a = graph.TryFieldInput(model, PortKeys.A, width, height, out var alpha)
			? alpha!
			: FieldExtensions.Fill(width, height, baseColor.A);

		return new RgbaBitmap(r, g, b, a);
	}
}
