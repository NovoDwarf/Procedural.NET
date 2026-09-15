

using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Generators;

public sealed class ColorNode : BaseNode, IMultiColorExecutor, IMultiBitmapExecutor
{
	public override string Key => NodeKeys.ConstantColor;
	public override string GroupKey => NodeGroupKeys.Generators;
	public override string SubgroupKey => NodeSubgroupKeys.Constants;
	
	public override ColorF Color => new(0.42f, 0.34f, 0.54f);
	
	public override bool AllowParameterConnections => false;
	public override bool SupportsPreview => true;

	public override IReadOnlyList<GraphNodePort> Inputs => [];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.Color, ValueShape.Color, PortSemantics.Color)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.R, 0f, 1f, 0.01f, 1f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.G, 0f, 1f, 0.01f, 1f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.B, 0f, 1f, 0.01f, 1f,
			Category: ParameterCategory.Primary),
		new(ParameterKeys.A, 0f, 1f, 0.01f, 1f,
			Category: ParameterCategory.Primary)
	];

	public RgbaColor EvaluateColor(GraphNode model, IGraphExecutionContext graph, string outputKey, EvaluationContext context)
	{
		return new RgbaColor(
			graph.GetParameter(model, ParameterKeys.R, context),
			graph.GetParameter(model, ParameterKeys.G, context),
			graph.GetParameter(model, ParameterKeys.B, context),
			graph.GetParameter(model, ParameterKeys.A, context)).Clamp();
	}

	public RgbaBitmap EvaluateBitmap(GraphNode model, IGraphExecutionContext graph, string outputKey, int width, int height)
	{
		return RgbaBitmap.Fill(width, height, EvaluateColor(model, graph, outputKey, new EvaluationContext(0.5f, 0.5f)));
	}
}
