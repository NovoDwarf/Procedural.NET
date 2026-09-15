
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Display;

public sealed class DisplayDensityNode : BaseNode, IFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs =
	[
		new(PortKeys.Mask, ValueShape.Field, PortSemantics.Generic)
	];

	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs =
	[
		new(PortKeys.Preview, ValueShape.Field, PortSemantics.Density)
	];
	
	public override string Key => NodeKeys.DisplayDensity;
	public override string GroupKey => NodeGroupKeys.Display;
	
	public override ColorF Color => new(0.42f, 0.62f, 0.34f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	
	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		return graph.RequireFieldInput(model, PortKeys.Mask, width, height);
	}
}
