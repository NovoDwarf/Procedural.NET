using NovoDwarf.Primitives.Models; 
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Core.Execution;

internal static class GraphNodeEvaluationAdapter
{
	public static GraphValue Evaluate(
		INodeExecutor executor,
		GraphNode graphNode,
		IGraphExecutionContext graph,
		GraphEvaluationRequest request)
	{
		if (request.Domain is EvaluationDomain.Sample sample)
			if (ShouldEvaluateColor(graphNode, request.OutputKey))
				return new GraphValue.Color(EvaluateColor(executor, graphNode, graph, request.OutputKey, new EvaluationContext(sample.U, sample.V)));
			else
				return new GraphValue.Scalar(EvaluateScalar(executor, graphNode, graph, request.OutputKey, new EvaluationContext(sample.U, sample.V)));

		if (request.Domain is not EvaluationDomain.Raster raster)
			throw new NotSupportedException($"Unsupported graph evaluation domain [{request.Domain.GetType().Name}].");
		
		if (ShouldEvaluatePoints(graphNode, request.OutputKey))
			return new GraphValue.Points(EvaluatePoints(executor, graphNode, graph, request.OutputKey, raster.Width, raster.Height));
		
		if (ShouldEvaluatePaths(graphNode, request.OutputKey))
			return new GraphValue.Paths(EvaluatePaths(executor, graphNode, graph, request.OutputKey, raster.Width, raster.Height));
		
		if (ShouldEvaluateBitmap(executor, graphNode, request.OutputKey) || ShouldEvaluateColor(graphNode, request.OutputKey))
			return new GraphValue.Bitmap(EvaluateBitmap(executor, graphNode, graph, request.OutputKey, raster.Width, raster.Height));
		
		return new GraphValue.Raster(EvaluateRaster(executor, graphNode, graph, request.OutputKey, raster.Width, raster.Height));
	}

	private static bool ShouldEvaluateColor(GraphNode graphNode, string outputKey)
	{
		if (string.IsNullOrEmpty(outputKey))
			return false;

		return graphNode.Outputs.FirstOrDefault(output => output.Key == outputKey).Shape == ValueShape.Color;
	}

	private static bool ShouldEvaluateBitmap(INodeExecutor executor, GraphNode graphNode, string outputKey)
	{
		if (string.IsNullOrEmpty(outputKey))
			return executor is IBitmapExecutor or IMultiBitmapExecutor;

		return graphNode.Outputs.FirstOrDefault(output => output.Key == outputKey).Shape == ValueShape.Bitmap;
	}

	private static bool ShouldEvaluatePoints(GraphNode graphNode, string outputKey)
		=> graphNode.Outputs.FirstOrDefault(output => output.Key == outputKey).Shape == ValueShape.Points;

	private static bool ShouldEvaluatePaths(GraphNode graphNode, string outputKey)
		=> graphNode.Outputs.FirstOrDefault(output => output.Key == outputKey).Shape == ValueShape.Paths;

	private static Field EvaluateRaster(
		INodeExecutor executor,
		GraphNode graphNode,
		IGraphExecutionContext graph,
		string outputKey,
		int width,
		int height)
	{
		if (executor is IMultiFieldExecutor mf)
			return mf.EvaluateField(graphNode, graph, outputKey, width, height);

		if (executor is IFieldExecutor f)
			return f.EvaluateField(graphNode, graph, width, height);

		if (executor is IMultiSampledExecutor ms)
			return ExpandScalarMulti(ms, graphNode, graph, outputKey, width, height);

		if (executor is ISampledExecutor s)
			return ExpandScalar(s, graphNode, graph, width, height);

		throw new InvalidOperationException($"Node [{graphNode.Title}] does not implement a raster output.");
	}

	private static RgbaBitmap EvaluateBitmap(
		INodeExecutor executor,
		GraphNode graphNode,
		IGraphExecutionContext graph,
		string outputKey,
		int width,
		int height)
	{
		if (executor is IMultiBitmapExecutor mb)
			return mb.EvaluateBitmap(graphNode, graph, outputKey, width, height);

		if (executor is IBitmapExecutor b)
			return b.EvaluateBitmap(graphNode, graph, width, height);

		if (executor is IMultiColorExecutor mc)
			return RgbaBitmap.Fill(width, height, mc.EvaluateColor(graphNode, graph, outputKey, new EvaluationContext(0.5f, 0.5f)));

		if (executor is IColorExecutor c)
			return RgbaBitmap.Fill(width, height, c.EvaluateColor(graphNode, graph, new EvaluationContext(0.5f, 0.5f)));

		throw new InvalidOperationException($"Node [{graphNode.Title}] does not implement a bitmap output.");
	}

	private static PointSet EvaluatePoints(
		INodeExecutor executor,
		GraphNode graphNode,
		IGraphExecutionContext graph,
		string outputKey,
		int width,
		int height)
	{
		if (executor is IMultiPointSetExecutor mp)
			return mp.EvaluatePoints(graphNode, graph, outputKey, width, height);

		if (executor is IPointSetExecutor p)
			return p.EvaluatePoints(graphNode, graph, width, height);

		throw new InvalidOperationException($"Node [{graphNode.Title}] does not implement a point-set output.");
	}

	private static PathSet EvaluatePaths(
		INodeExecutor executor,
		GraphNode graphNode,
		IGraphExecutionContext graph,
		string outputKey,
		int width,
		int height)
	{
		if (executor is IMultiPathSetExecutor mp)
			return mp.EvaluatePaths(graphNode, graph, outputKey, width, height);

		if (executor is IPathSetExecutor p)
			return p.EvaluatePaths(graphNode, graph, width, height);

		throw new InvalidOperationException($"Node [{graphNode.Title}] does not implement a path-set output.");
	}

	private static RgbaColor EvaluateColor(
		INodeExecutor executor,
		GraphNode graphNode,
		IGraphExecutionContext graph,
		string outputKey,
		EvaluationContext context)
	{
		if (executor is IMultiColorExecutor mc)
			return mc.EvaluateColor(graphNode, graph, outputKey, context).Clamp();

		if (executor is IColorExecutor c)
			return c.EvaluateColor(graphNode, graph, context).Clamp();

		throw new InvalidOperationException($"Node [{graphNode.Title}] does not implement a color output.");
	}

	private static float EvaluateScalar(
		INodeExecutor executor,
		GraphNode graphNode,
		IGraphExecutionContext graph,
		string outputKey,
		EvaluationContext context)
	{
		if (executor is IMultiSampledExecutor ms)
			return ms.EvaluatePoint(graphNode, graph, outputKey, context);

		if (executor is ISampledExecutor s)
			return s.EvaluatePoint(graphNode, graph, context);

		throw new InvalidOperationException($"Node [{graphNode.Title}] does not implement a sample output.");
	}

	private static Field ExpandScalar(ISampledExecutor executor, GraphNode graphNode, IGraphExecutionContext graph, int width, int height)
	{
		var raster = new Field(width, height);
		
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var u = width  <= 1 ? 0f : x / (float)(width  - 1);
			var v = height <= 1 ? 0f : y / (float)(height - 1);
			
			raster[x, y] = executor.EvaluatePoint(graphNode, graph, new EvaluationContext(u, v));
		}
		
		return raster;
	}

	private static Field ExpandScalarMulti(IMultiSampledExecutor executor, GraphNode graphNode, IGraphExecutionContext graph, string outputKey, int width, int height)
	{
		var raster = new Field(width, height);
		
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var u = width  <= 1 ? 0f : x / (float)(width  - 1);
			var v = height <= 1 ? 0f : y / (float)(height - 1);
			
			raster[x, y] = executor.EvaluatePoint(graphNode, graph, outputKey, new EvaluationContext(u, v));
		}
		
		return raster;
	}
}
