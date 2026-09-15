using System.Collections.Concurrent;

using NovoDwarf.Primitives.Models;

using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;
using Procedural.NET.Storage;
using Procedural.NET.Storage.Interfaces;
using Procedural.NET.Storage.Macros;

namespace Procedural.NET.Nodes.Macros;

public sealed class MacroNode : BaseNode
{
	private readonly IServiceProvider _services;
	private readonly ConcurrentDictionary<string, MacroDefinition?> _definitionCache = new(StringComparer.OrdinalIgnoreCase);

	public MacroNode(IServiceProvider services)
	{
		_services = services;
	}

	public override string Key => NodeKeys.MacroNode;
	public override string GroupKey => NodeGroupKeys.Utilities;
	public override string SubgroupKey => NodeSubgroupKeys.Macro;
	
	public override ColorF Color => new(0.56f, 0.42f, 0.20f);

	public override IReadOnlyList<GraphNodePort> Inputs => [];
	public override IReadOnlyList<GraphNodePort> Outputs => [];
	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Path, 0f, 0f, 0f, 0f, Kind: ParameterKind.String, Category: ParameterCategory.Primary)
	];

	public bool UpdatePorts(GraphNode model)
	{
		var path = model.GetString(ParameterKeys.Path);
		var definition = GetDefinition(path);
		_definitionCache[path] = definition;
		var parameters = BuildParameters(definition);
		var inputs = BuildInputs(definition);
		var outputs = BuildOutputs(definition);

		var parametersChanged = model.ReplaceParameterDefinitions(parameters);
		var inputsChanged = model.ReplaceInputs(inputs);
		var outputsChanged = model.ReplaceOutputs(outputs);
		return parametersChanged || inputsChanged || outputsChanged;
	}

	public GraphValue Evaluate(GraphNode model, IGraphExecutionContext graph, GraphEvaluationRequest request)
	{
		var path = model.GetString(ParameterKeys.Path);
		var definition = GetDefinition(path);
		if (definition is null)
			return DefaultValue(model, request.OutputKey, request, null);

		var view = definition.Views.FirstOrDefault(contract => MacroNodeContracts.PreviewPortKey(contract.NodeId) == request.OutputKey);
		if (view is not null)
			return EvaluateContract(definition, model, graph, request, view.NodeId, view.Shape);

		var inner = new GraphDocument();
		var sessions = GetSessionService();
		sessions.Apply(inner, definition.Session);
		var (bindings, portBindings) = BuildBindings(model, graph, request, definition);
		var output = definition.Outputs.FirstOrDefault(contract => MacroNodeContracts.OutputPortKey(contract.NodeId) == request.OutputKey);
		if (output is null || !inner.TryGetInputConnection(output.NodeId, PortKeys.Source, out var source))
			return DefaultValue(model, request.OutputKey, request, definition);

		foreach (var nodeId in bindings.Keys)
			inner.NotifyNodeChanged(nodeId);

		using var _ = MacroEvaluationScope.Push(new MacroEvaluationBindings(bindings, portBindings), path);
		var runtime = new GraphExecutionContext(inner);
		return runtime.Evaluate(new GraphEvaluationRequest(inner.GetNode(source.NodeId), source.PortKey, request.Domain));
	}
	
	private IMacroLibrary GetMacroLibrary() =>
		_services.GetService(typeof(IMacroLibrary)) as IMacroLibrary
		?? throw new InvalidOperationException($"{nameof(IMacroLibrary)} is not registered.");

	private GraphSessionService GetSessionService() =>
		_services.GetService(typeof(GraphSessionService)) as GraphSessionService
		?? throw new InvalidOperationException($"{nameof(GraphSessionService)} is not registered.");

	private MacroDefinition? GetDefinition(string path)
	{
		if (_definitionCache.TryGetValue(path, out var cached))
			return cached;

		var definition = GetMacroLibrary().TryLoad(path);
		_definitionCache[path] = definition;
		return definition;
	}

	private GraphValue EvaluateContract(
		MacroDefinition definition,
		GraphNode model,
		IGraphExecutionContext graph,
		GraphEvaluationRequest request,
		Guid targetNodeId,
		string fallbackShape)
	{
		var inner = new GraphDocument();
		var sessions = GetSessionService();
		sessions.Apply(inner, definition.Session);
		var (bindings, portBindings) = BuildBindings(model, graph, request, definition);
		
		if (!inner.TryGetInputConnection(targetNodeId, PortKeys.Source, out var source))
			return DefaultValueByShape(ParseShape(fallbackShape), request);

		foreach (var nodeId in bindings.Keys)
			inner.NotifyNodeChanged(nodeId);

		using var _ = MacroEvaluationScope.Push(new MacroEvaluationBindings(bindings, portBindings), model.GetString(ParameterKeys.Path));
		var runtime = new GraphExecutionContext(inner);
		
		return runtime.Evaluate(new GraphEvaluationRequest(inner.GetNode(source.NodeId), source.PortKey, request.Domain));
	}

	private static IReadOnlyList<GraphNodeParameter> BuildParameters(MacroDefinition? definition)
	{
		var parameters = new List<GraphNodeParameter>
		{
			new(ParameterKeys.Path, 0f, 0f, 0f, 0f, Kind: ParameterKind.String, Category: ParameterCategory.Primary)
		};

		if (definition is null)
			return parameters;

		parameters.AddRange(definition.Parameters.Select(contract => new GraphNodeParameter(
			MacroNodeContracts.ParameterKey(contract.NodeId, contract.PortKey),
			contract.Min,
			contract.Max,
			Math.Max(contract.Step, 0.0001f),
			contract.DefaultValue,
			Section: MacroNodeContracts.MacroParameterSection,
			UseSlider: contract.Shape != nameof(ValueShape.Boolean),
			Kind: ParseShape(contract.Shape) == ValueShape.Boolean ? ParameterKind.Checkbox : ParameterKind.Number,
			Category: ParameterCategory.Primary, CustomLocKey: contract.Name)));

		return parameters;
	}

	private static IReadOnlyList<GraphNodePort> BuildInputs(MacroDefinition? definition) =>
		definition?.Inputs.Select(contract =>
			new GraphNodePort(
				MacroNodeContracts.InputPortKey(contract.NodeId),
				ParseShape(contract.Shape),
				ParseSemantics(contract.Semantics), contract.Name)).ToArray() ?? [];

	private static IReadOnlyList<GraphNodePort> BuildOutputs(MacroDefinition? definition) =>
		definition?.Outputs.Select(contract =>
			new GraphNodePort(
				MacroNodeContracts.OutputPortKey(contract.NodeId),
				ParseShape(contract.Shape),
				ParseSemantics(contract.Semantics), contract.Name)).ToArray() ?? [];

	private static (Dictionary<Guid, GraphValue> NodeValues, Dictionary<string, GraphValue> PortValues) BuildBindings(
		GraphNode model,
		IGraphExecutionContext graph,
		GraphEvaluationRequest request,
		MacroDefinition definition)
	{
		var bindings = new Dictionary<Guid, GraphValue>();
		var portBindings = new Dictionary<string, GraphValue>(StringComparer.Ordinal);
		
		var sampleContext = request.Domain is EvaluationDomain.Sample sample
			? new EvaluationContext(sample.U, sample.V)
			: new EvaluationContext(0.5f, 0.5f);
		
		var rasterDomain = request.Domain as EvaluationDomain.Raster;

		foreach (var input in definition.Inputs)
		{
			var portKey = MacroNodeContracts.InputPortKey(input.NodeId);
			var shape = ParseShape(input.Shape);
			switch (shape)
			{
				case ValueShape.Points:
					if (rasterDomain is not null && graph.TryPointSetInput(model, portKey, rasterDomain.Width, rasterDomain.Height, out var points))
						bindings[input.NodeId] = new GraphValue.Points(points!);
					break;
				case ValueShape.Paths:
					if (rasterDomain is not null && graph.TryPathSetInput(model, portKey, rasterDomain.Width, rasterDomain.Height, out var paths))
						bindings[input.NodeId] = new GraphValue.Paths(paths!);
					break;
				case ValueShape.Bitmap:
					if (rasterDomain is not null && graph.TryBitmapInput(model, portKey, rasterDomain.Width, rasterDomain.Height, out var bitmap))
						bindings[input.NodeId] = new GraphValue.Bitmap(bitmap!);
					else if (graph.TryColorInput(model, portKey, sampleContext, out var sampledBitmapColor))
						bindings[input.NodeId] = new GraphValue.Color(sampledBitmapColor!);
					break;
				case ValueShape.Color:
					if (graph.TryColorInput(model, portKey, sampleContext, out var color))
						bindings[input.NodeId] = new GraphValue.Color(color!);
					break;
				case ValueShape.Field:
					if (rasterDomain is not null && graph.TryFieldInput(model, portKey, rasterDomain.Width, rasterDomain.Height, out var field))
						bindings[input.NodeId] = new GraphValue.Raster(field!);
					else if (graph.TryScalarInput(model, portKey, sampleContext, out var sampledFieldScalar))
						bindings[input.NodeId] = new GraphValue.Scalar(sampledFieldScalar);
					break;
				default:
					if (graph.TryScalarInput(model, portKey, sampleContext, out var scalar))
						bindings[input.NodeId] = new GraphValue.Scalar(scalar);
					break;
			}
		}

		foreach (var parameter in definition.Parameters)
		{
			var value = graph.GetParameter(model, MacroNodeContracts.ParameterKey(parameter.NodeId, parameter.PortKey), sampleContext);
			portBindings[MacroNodeContracts.ParameterBindingKey(parameter.NodeId, parameter.PortKey)] = new GraphValue.Scalar(value);
		}

		return (bindings, portBindings);
	}

	private static GraphValue DefaultValue(GraphNode model, string outputKey, GraphEvaluationRequest request, MacroDefinition? definition)
	{
		var shape = model.Outputs.FirstOrDefault(output => output.Key == outputKey).Shape;
		
		var preview = definition?.Views.FirstOrDefault(view => MacroNodeContracts.PreviewPortKey(view.NodeId) == outputKey);
		shape = preview is null ? ValueShape.Float : ParseShape(preview.Shape);

		return DefaultValueByShape(shape, request);
	}

	private static GraphValue DefaultValueByShape(ValueShape shape, GraphEvaluationRequest request)
	{
		return request.Domain switch
		{
			EvaluationDomain.Sample when shape == ValueShape.Color => new GraphValue.Color(new RgbaColor(0f, 0f, 0f, 1f)),
			EvaluationDomain.Sample => new GraphValue.Scalar(0f),
			EvaluationDomain.Raster raster when shape == ValueShape.Points => new GraphValue.Points(PointSet.Empty(raster.Width, raster.Height)),
			EvaluationDomain.Raster raster when shape == ValueShape.Paths => new GraphValue.Paths(PathSet.Empty(raster.Width, raster.Height)),
			EvaluationDomain.Raster raster when shape is ValueShape.Bitmap or ValueShape.Color => new GraphValue.Bitmap(RgbaBitmap.Fill(raster.Width, raster.Height, new RgbaColor(0f, 0f, 0f, 1f))),
			EvaluationDomain.Raster raster => new GraphValue.Raster(new Field(raster.Width, raster.Height)),
			_ => new GraphValue.Scalar(0f)
		};
	}

	private static ValueShape ParseShape(string shape) =>
		Enum.TryParse<ValueShape>(shape, ignoreCase: true, out var parsed) ? parsed : ValueShape.Float;

	private static PortSemantics ParseSemantics(string semantics) =>
		Enum.TryParse<PortSemantics>(semantics, ignoreCase: true, out var parsed) ? parsed : PortSemantics.Generic;
}
