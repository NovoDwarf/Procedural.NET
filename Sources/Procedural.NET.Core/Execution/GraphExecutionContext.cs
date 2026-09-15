using NovoDwarf.Primitives.Models;
using Procedural.NET.Core.Execution.Interfaces;
using Procedural.NET.Core.Utilities;

namespace Procedural.NET.Core.Execution;

public sealed class GraphExecutionContext : IGraphExecutionContext
{
	private readonly IGraphRuntimeDocument _document;
	private readonly HashSet<(Guid NodeId, string OutputKey)> _stack = [];

	public GraphExecutionContext(IGraphRuntimeDocument document)
	{
		_document = document;
	}

	public GraphMetadata Metadata => _document.Metadata;

	public GraphValue Evaluate(GraphEvaluationRequest request) =>
		EvaluateOutput(request.Node, request.OutputKey, request);

	public Field RequireFieldInput(GraphNode node, string inputKey, int width, int height)
	{
		if (TryFieldInput(node, inputKey, width, height, out var field))
			return field!;

		throw new InvalidOperationException($"Node [{node.Title}] requires a connected field input [{inputKey}].");
	}

	public bool TryFieldInput(GraphNode node, string inputKey, int width, int height, out Field? field)
	{
		field = null;
		if (!_document.TryGetInputConnection(node.Id, inputKey, out var output))
			return false;

		field = EvaluateFieldOutput(_document.GetNode(output.NodeId), output.PortKey, width, height);
		
		return true;
	}

	public PointSet RequirePointSetInput(GraphNode node, string inputKey, int width, int height)
	{
		if (TryPointSetInput(node, inputKey, width, height, out var points))
			return points!;

		throw new InvalidOperationException($"Node [{node.Title}] requires a connected point-set input [{inputKey}].");
	}

	public bool TryPointSetInput(GraphNode node, string inputKey, int width, int height, out PointSet? points)
	{
		points = null;
		if (!_document.TryGetInputConnection(node.Id, inputKey, out var output))
			return false;

		points = EvaluatePointSetOutput(_document.GetNode(output.NodeId), output.PortKey, width, height);
		return true;
	}

	public PathSet RequirePathSetInput(GraphNode node, string inputKey, int width, int height)
	{
		if (TryPathSetInput(node, inputKey, width, height, out var paths))
			return paths!;

		throw new InvalidOperationException($"Node [{node.Title}] requires a connected path-set input [{inputKey}].");
	}

	public bool TryPathSetInput(GraphNode node, string inputKey, int width, int height, out PathSet? paths)
	{
		paths = null;
		if (!_document.TryGetInputConnection(node.Id, inputKey, out var output))
			return false;

		paths = EvaluatePathSetOutput(_document.GetNode(output.NodeId), output.PortKey, width, height);
		return true;
	}

	public bool TryValueInput(GraphNode node, string inputKey, int width, int height, out GraphValue? value)
	{
		value = null;
		if (!_document.TryGetInputConnection(node.Id, inputKey, out var output))
			return false;

		value = EvaluateOutput(
			_document.GetNode(output.NodeId),
			output.PortKey,
			new GraphEvaluationRequest(_document.GetNode(output.NodeId), output.PortKey, new EvaluationDomain.Raster(width, height)));
		return true;
	}

	public RgbaBitmap RequireBitmapInput(GraphNode node, string inputKey, int width, int height)
	{
		if (TryBitmapInput(node, inputKey, width, height, out var bitmap))
			return bitmap!;

		throw new InvalidOperationException($"Node [{node.Title}] requires a connected bitmap input [{inputKey}].");
	}

	public bool TryBitmapInput(GraphNode node, string inputKey, int width, int height, out RgbaBitmap? bitmap)
	{
		bitmap = null;
		if (!_document.TryGetInputConnection(node.Id, inputKey, out var output))
			return false;

		bitmap = EvaluateBitmapOutput(_document.GetNode(output.NodeId), output.PortKey, width, height);
		
		return true;
	}

	public RgbaColor RequireColorInput(GraphNode node, string inputKey, EvaluationContext context)
	{
		if (TryColorInput(node, inputKey, context, out var color))
			return color!;

		throw new InvalidOperationException($"Node [{node.Title}] requires a connected color input [{inputKey}].");
	}

	public bool TryColorInput(GraphNode node, string inputKey, EvaluationContext context, out RgbaColor? color)
	{
		color = null;
		if (!_document.TryGetInputConnection(node.Id, inputKey, out var output))
			return false;

		color = EvaluateColorOutput(_document.GetNode(output.NodeId), output.PortKey, context);
		
		return true;
	}

	public float RequireScalarInput(GraphNode node, string inputKey, EvaluationContext context)
	{
		if (TryScalarInput(node, inputKey, context, out var value))
			return value;

		throw new InvalidOperationException($"Node [{node.Title}] requires a connected scalar input [{inputKey}].");
	}

	public bool TryScalarInput(GraphNode node, string inputKey, EvaluationContext context, out float value)
	{
		value = 0;

		if (!TryGetInput(node, inputKey, out var source, out var port))
			return false;

		value = EvaluateScalarOutput(source, port, context);
		return true;
	}

	public float GetParameter(GraphNode node, string parameterKey, EvaluationContext context)
	{
		if (!_document.TryGetInputConnection(node.Id, GraphRules.ParameterPortKey(parameterKey), out var output))
			return node.Parameters.Get(parameterKey);

		return EvaluateScalarOutput(_document.GetNode(output.NodeId), output.PortKey, context);
	}

	public T GetParameter<T>(GraphNode node, string parameterKey, EvaluationContext context)
	{
		var value = GetParameter(node, parameterKey, context);
	
		return BasicParameterConverter.Convert<T>(value, parameterKey);
	}

	public float GetParameter(GraphNode node, string parameterKey, float u, float v)
		=> GetParameter(node, parameterKey, new EvaluationContext(u, v));

	public T GetParameter<T>(GraphNode node, string parameterKey, float u, float v)
		=> GetParameter<T>(node, parameterKey, new EvaluationContext(u, v));

	private Field EvaluateFieldOutput(GraphNode node, string outputKey, int width, int height)
	{
		if (_document.TryGetCachedField(node.Id, outputKey, width, height, out var cached))
			return cached;

		var value = EvaluateOutput(
			node,
			outputKey,
			new GraphEvaluationRequest(node, outputKey, new EvaluationDomain.Raster(width, height)));
		if (value is not GraphValue.Raster rasterValue)
			throw new InvalidOperationException($"Node [{node.Title}] returned [{value.GetType().Name}] for raster request.");

		_document.SetCachedField(node.Id, outputKey, width, height, rasterValue.Value);
		return rasterValue.Value;
	}

	private RgbaBitmap EvaluateBitmapOutput(GraphNode node, string outputKey, int width, int height)
	{
		var value = EvaluateOutput(
			node,
			outputKey,
			new GraphEvaluationRequest(node, outputKey, new EvaluationDomain.Raster(width, height)));

		if (value is GraphValue.Bitmap bitmapValue) 
			return bitmapValue.Value;
		
		if (value is GraphValue.Color colorValue)
			return RgbaBitmap.Fill(width, height, colorValue.Value);

		throw new InvalidOperationException($"Node [{node.Title}] returned [{value.GetType().Name}] for bitmap request.");

	}

	private PointSet EvaluatePointSetOutput(GraphNode node, string outputKey, int width, int height)
	{
		if (_document.TryGetCachedPointSet(node.Id, outputKey, width, height, out var cached))
			return cached;

		var value = EvaluateOutput(
			node,
			outputKey,
			new GraphEvaluationRequest(node, outputKey, new EvaluationDomain.Raster(width, height)));
		if (value is not GraphValue.Points pointsValue)
			throw new InvalidOperationException($"Node [{node.Title}] returned [{value.GetType().Name}] for point-set request.");

		_document.SetCachedPointSet(node.Id, outputKey, width, height, pointsValue.Value);
		return pointsValue.Value;
	}

	private PathSet EvaluatePathSetOutput(GraphNode node, string outputKey, int width, int height)
	{
		if (_document.TryGetCachedPathSet(node.Id, outputKey, width, height, out var cached))
			return cached;

		var value = EvaluateOutput(
			node,
			outputKey,
			new GraphEvaluationRequest(node, outputKey, new EvaluationDomain.Raster(width, height)));
		if (value is not GraphValue.Paths pathsValue)
			throw new InvalidOperationException($"Node [{node.Title}] returned [{value.GetType().Name}] for path-set request.");

		_document.SetCachedPathSet(node.Id, outputKey, width, height, pathsValue.Value);
		return pathsValue.Value;
	}

	private RgbaColor EvaluateColorOutput(GraphNode node, string outputKey, EvaluationContext context)
	{
		var value = EvaluateOutput(
			node,
			outputKey,
			new GraphEvaluationRequest(node, outputKey, new EvaluationDomain.Sample(context.U, context.V)));
		
		return value is GraphValue.Color color
			? color.Value
			: throw new InvalidOperationException($"Node [{node.Title}] returned [{value.GetType().Name}] for color request.");
	}

	private float EvaluateScalarOutput(GraphNode node, string outputKey, EvaluationContext context)
	{
		var value = EvaluateOutput(
			node,
			outputKey,
			new GraphEvaluationRequest(node, outputKey, new EvaluationDomain.Sample(context.U, context.V)));
		
		return value is GraphValue.Scalar scalar
			? scalar.Value
			: throw new InvalidOperationException($"Node [{node.Title}] returned [{value.GetType().Name}] for sample request.");
	}

	private GraphValue EvaluateOutput(GraphNode node, string outputKey, GraphEvaluationRequest request)
	{
		if (!_stack.Add((node.Id, outputKey)))
			throw new InvalidOperationException($"Generator graph has a cycle at node [{node.Id}] port [{outputKey}].");

		try
		{
			return node.Executor.Evaluate(node, this, request);
		}
		finally
		{
			_stack.Remove((node.Id, outputKey));
		}
	}
	
	private bool TryGetInput(
		GraphNode node,
		string inputKey,
		out GraphNode sourceNode,
		out string outputKey)
	{
		sourceNode = null!;
		outputKey = null!;

		if (!_document.TryGetInputConnection(node.Id, inputKey, out var output))
			return false;

		sourceNode = _document.GetNode(output.NodeId);
		outputKey = output.PortKey;
		return true;
	}
	
	private T Require<T>(GraphNode node, string inputKey, Func<GraphNode, string, (bool Success, T Value)> getter)
	{
		var result = getter(node, inputKey);

		if (result.Success)
			return result.Value;

		throw new InvalidOperationException(
			$"Node [{node.Title}] requires a connected input [{inputKey}].");
	}
	
	private T Evaluate<T>(GraphNode node, string outputKey, GraphEvaluationRequest request, Func<GraphValue, T?> converter) where T : class
	{
		var value = EvaluateOutput(node, outputKey, request);
		var result = converter(value);

		return result ?? throw new InvalidOperationException("...");
	}
}
