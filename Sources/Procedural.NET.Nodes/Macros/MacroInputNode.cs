
using NovoDwarf.Primitives.Models;

using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Macros;

public sealed class MacroInputNode : BaseNode, ISampledExecutor, IFieldExecutor, IBitmapExecutor, IColorExecutor, IPointSetExecutor, IPathSetExecutor
{
	private const string DefaultPortName = "Input";

	public override string Key => NodeKeys.MacroInput;
	public override string GroupKey => NodeGroupKeys.Utilities;
	public override string SubgroupKey => NodeSubgroupKeys.Macro;
	
	public override ColorF Color => new(0.46f, 0.44f, 0.28f);
	
	public override bool AllowParameterConnections => false;

	public override IReadOnlyList<GraphNodePort> Inputs => [];

	public override IReadOnlyList<GraphNodePort> Outputs => [];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.DisplayName, 0f, 0f, 0f, 0f,
			Kind: ParameterKind.String, Category: ParameterCategory.Primary),
		new(ParameterKeys.RuntimeShape, 0f, 7f, 1f, 3f,
			UseSlider: false, Kind: ParameterKind.Option, Options: MacroNodeContracts.RuntimeShapeOptions, Category: ParameterCategory.Primary),
		new(ParameterKeys.RuntimeSemantics, 0f, 9f, 1f, 1f,
			UseSlider: false, Kind: ParameterKind.Option, Options: MacroNodeContracts.RuntimeSemanticsOptions, Category: ParameterCategory.Primary),
		new(ParameterKeys.Value, 0f, 1f, 0.01f, 0f,
			Category: ParameterCategory.Advanced)
	];

	public bool UpdatePorts(GraphNode model)
	{
		var label = string.IsNullOrWhiteSpace(model.GetString(ParameterKeys.DisplayName))
			? DefaultPortName
			: model.GetString(ParameterKeys.DisplayName);
		var shape = MacroNodeContracts.ResolveRuntimeShape(model);
		var semantics = MacroNodeContracts.ResolveRuntimeSemantics(model);
		return model.ReplaceOutputs([new GraphNodePort(PortKeys.Value, shape, semantics, label)]);
	}

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context) =>
		MacroEvaluationScope.TryGetValue(model.Id, out var scoped)
			? MacroNodeContracts.ReadScalar(scoped, context)
			: Math.Clamp(graph.GetParameter(model, ParameterKeys.Value, context), 0f, 1f);

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		if (MacroEvaluationScope.TryGetValue(model.Id, out var scoped))
			return MacroNodeContracts.ReadField(scoped, width, height);

		var value = Math.Clamp(graph.GetParameter(model, ParameterKeys.Value, 0.5f, 0.5f), 0f, 1f);
		var result = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			result[x, y] = value;
		return result;
	}

	public RgbaBitmap EvaluateBitmap(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		if (MacroEvaluationScope.TryGetValue(model.Id, out var scoped))
			return MacroNodeContracts.ReadBitmap(scoped, width, height);

		var value = Math.Clamp(graph.GetParameter(model, ParameterKeys.Value, 0.5f, 0.5f), 0f, 1f);
		return RgbaBitmap.Fill(width, height, new RgbaColor(value, value, value, 1f));
	}

	public RgbaColor EvaluateColor(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		if (MacroEvaluationScope.TryGetValue(model.Id, out var scoped))
			return MacroNodeContracts.ReadColor(scoped, context);

		var value = Math.Clamp(graph.GetParameter(model, ParameterKeys.Value, context), 0f, 1f);
		return new RgbaColor(value, value, value, 1f);
	}

	public PointSet EvaluatePoints(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		if (MacroEvaluationScope.TryGetValue(model.Id, out var scoped))
			return MacroNodeContracts.ReadPoints(scoped, width, height);

		return PointSet.Empty(width, height);
	}

	public PathSet EvaluatePaths(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		if (MacroEvaluationScope.TryGetValue(model.Id, out var scoped))
			return MacroNodeContracts.ReadPaths(scoped, width, height);

		return PathSet.Empty(width, height);
	}
}
