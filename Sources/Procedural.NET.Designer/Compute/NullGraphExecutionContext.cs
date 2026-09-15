using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Designer.Compute;

internal sealed class NullGraphExecutionContext : IGraphExecutionContext
{
	public GraphMetadata Metadata { get; } = new();

	public GraphValue Evaluate(GraphEvaluationRequest request) => request.Node.Executor.Evaluate(request.Node, this, request);
	public Field RequireFieldInput(GraphNode graphNode, string inputKey, int width, int height) => throw MissingInput(graphNode, inputKey);
	public bool TryFieldInput(GraphNode graphNode, string inputKey, int width, int height, out Field? field) { field = null; return false; }
	public PointSet RequirePointSetInput(GraphNode graphNode, string inputKey, int width, int height) => throw MissingInput(graphNode, inputKey);
	public bool TryPointSetInput(GraphNode graphNode, string inputKey, int width, int height, out PointSet? points) { points = null; return false; }
	public PathSet RequirePathSetInput(GraphNode graphNode, string inputKey, int width, int height) => throw MissingInput(graphNode, inputKey);
	public bool TryPathSetInput(GraphNode graphNode, string inputKey, int width, int height, out PathSet? paths) { paths = null; return false; }
	public bool TryValueInput(GraphNode graphNode, string inputKey, int width, int height, out GraphValue? value) { value = null; return false; }
	public RgbaBitmap RequireBitmapInput(GraphNode graphNode, string inputKey, int width, int height) => throw MissingInput(graphNode, inputKey);
	public bool TryBitmapInput(GraphNode graphNode, string inputKey, int width, int height, out RgbaBitmap? bitmap) { bitmap = null; return false; }
	public RgbaColor RequireColorInput(GraphNode graphNode, string inputKey, EvaluationContext context) => throw MissingInput(graphNode, inputKey);
	public bool TryColorInput(GraphNode graphNode, string inputKey, EvaluationContext context, out RgbaColor? color) { color = null; return false; }
	public float RequireScalarInput(GraphNode graphNode, string inputKey, EvaluationContext context) => throw MissingInput(graphNode, inputKey);
	public bool TryScalarInput(GraphNode graphNode, string inputKey, EvaluationContext context, out float value) { value = 0; return false; }
	public float GetParameter(GraphNode graphNode, string parameterKey, EvaluationContext context) => graphNode.Get(parameterKey);
	public T GetParameter<T>(GraphNode graphNode, string parameterKey, EvaluationContext context) => graphNode.Get<T>(parameterKey);
	public float GetParameter(GraphNode graphNode, string parameterKey, float u, float v) => graphNode.Get(parameterKey);
	public T GetParameter<T>(GraphNode graphNode, string parameterKey, float u, float v) => graphNode.Get<T>(parameterKey);

	private static InvalidOperationException MissingInput(GraphNode graphNode, string inputKey)
		=> new($"Node [{graphNode.Title}] requires connected input [{inputKey}].");
}
