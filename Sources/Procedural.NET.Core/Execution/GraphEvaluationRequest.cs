namespace Procedural.NET.Core.Execution;

public readonly record struct GraphEvaluationRequest(GraphNode Node, string OutputKey, EvaluationDomain Domain, GraphEvaluationQuality Quality = GraphEvaluationQuality.Accurate);
