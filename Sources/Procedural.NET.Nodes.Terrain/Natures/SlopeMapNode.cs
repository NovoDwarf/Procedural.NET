
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Terrain.Natures;

public sealed class SlopeMapNode : BaseNode, ISampledExecutor, IFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodePort> DefaultInputs = 	
	[
		new(PortKeys.Source, ValueShape.Field, PortSemantics.Terrain)
	];
	
	private static readonly IReadOnlyList<GraphNodePort> DefaultOutputs = 
	[
		new(PortKeys.Slope, ValueShape.Field, PortSemantics.Mask)
	];
	
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 
	[
		new(ParameterKeys.Scale, 0.1f, 16f, 0.1f, 1f,
			Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.SlopeMap;
	public override string GroupKey => NodeGroupKeys.Nature;
	
	public override ColorF Color => new(0.28f, 0.44f, 0.30f);

	public override IReadOnlyList<GraphNodePort> Inputs => DefaultInputs;
	public override IReadOnlyList<GraphNodePort> Outputs => DefaultOutputs;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		return 0f;
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		if (!graph.TryFieldInput(model, PortKeys.Source, width, height, out var source))
			return new Field(width, height);

		var scale = Math.Max(0.1f, graph.GetParameter(model, ParameterKeys.Scale, 0.5f, 0.5f));
		var result = new Field(width, height);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var xl = Math.Max(0, x - 1);
			var xr = Math.Min(width - 1, x + 1);
			var yu = Math.Max(0, y - 1);
			var yd = Math.Min(height - 1, y + 1);
			var dx = (source![xr, y] - source[xl, y]) / (xr - xl);
			var dy = (source[x, yd] - source[x, yu]) / (yd - yu);
			result[x, y] = Math.Clamp(MathF.Sqrt(dx * dx + dy * dy) * scale, 0f, 1f);
		}

		return result;
	}
}
