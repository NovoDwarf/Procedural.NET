
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Terrain.Display;

public sealed class DisplayWaterNode : BaseNode, IFieldExecutor
{
	public override string Key => NodeKeys.DisplayWater;
	public override string GroupKey => NodeGroupKeys.Display;
	
	public override ColorF Color => new(0.22f, 0.48f, 0.72f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.Mask, ValueShape.Field, PortSemantics.Water)
	];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.Preview, ValueShape.Field, PortSemantics.Water)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters => [];

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height) =>
		graph.RequireFieldInput(model, PortKeys.Mask, width, height);
}
