using NovoDwarf.Primitives.Models;


namespace Procedural.NET.Core.Execution.Interfaces;

public interface IGraphExecutionContext
{
	GraphMetadata Metadata { get; }

	GraphValue Evaluate(GraphEvaluationRequest request);

	Field RequireFieldInput(GraphNode graphNode, string inputKey, int width, int height);
	bool TryFieldInput(GraphNode graphNode, string inputKey, int width, int height, out Field? field);

	PointSet RequirePointSetInput(GraphNode graphNode, string inputKey, int width, int height);
	bool TryPointSetInput(GraphNode graphNode, string inputKey, int width, int height, out PointSet? points);

	PathSet RequirePathSetInput(GraphNode graphNode, string inputKey, int width, int height);
	bool TryPathSetInput(GraphNode graphNode, string inputKey, int width, int height, out PathSet? paths);

	bool TryValueInput(GraphNode graphNode, string inputKey, int width, int height, out GraphValue? value);

	RgbaBitmap RequireBitmapInput(GraphNode graphNode, string inputKey, int width, int height);
	bool TryBitmapInput(GraphNode graphNode, string inputKey, int width, int height, out RgbaBitmap? bitmap);

	RgbaColor RequireColorInput(GraphNode graphNode, string inputKey, EvaluationContext context);
	bool TryColorInput(GraphNode graphNode, string inputKey, EvaluationContext context, out RgbaColor? color);

	float RequireScalarInput(GraphNode graphNode, string inputKey, EvaluationContext context);
	bool TryScalarInput(GraphNode graphNode, string inputKey, EvaluationContext context, out float value);

	float GetParameter(GraphNode graphNode, string parameterKey, EvaluationContext context);
	T GetParameter<T>(GraphNode graphNode, string parameterKey, EvaluationContext context);
	float GetParameter(GraphNode graphNode, string parameterKey, float u, float v);
	T GetParameter<T>(GraphNode graphNode, string parameterKey, float u, float v);
}
