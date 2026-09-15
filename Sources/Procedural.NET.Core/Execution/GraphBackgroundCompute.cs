namespace Procedural.NET.Core.Execution;

public static class GraphBackgroundCompute
{
	private static readonly SemaphoreSlim Gate = new(initialCount: 1, maxCount: 1);
	
	public static async Task<GraphValue> EvaluateAsync(GraphDocument document, GraphNode graphNode, string outputKey, EvaluationDomain domain, CancellationToken cancellationToken)
	{
		var snapshot = GraphDocumentSnapshot.Capture(document);
		var request = new GraphEvaluationRequest(snapshot.GetNode(graphNode.Id), outputKey, domain);

		await Gate.WaitAsync(cancellationToken);

		try
		{
			return await Task.Run(() => new GraphExecutionContext(snapshot).Evaluate(request), cancellationToken);
		}
		finally
		{
			Gate.Release();
		}
	}
}
