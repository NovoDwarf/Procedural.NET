
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core.Enums;

namespace Procedural.NET.Core.Execution.Interfaces;

public interface INodeExecutor
{
	public string Key { get; }
	public string LocalizationKey { get; }
	public string GroupKey { get; }
	public string SubgroupKey => GroupKey;
	 
	public ColorF Color { get; }

	public IReadOnlyList<GraphNodePort> Inputs { get; }
	public IReadOnlyList<GraphNodePort> Outputs { get; }
	public IReadOnlyList<GraphNodeParameter> Parameters { get; }

	public bool AllowParameterConnections => true;
	public bool SupportsPreview => Outputs.Any(o => o.Shape is ValueShape.Color or ValueShape.Field or ValueShape.Bitmap or ValueShape.Volume or ValueShape.Points or ValueShape.Paths or ValueShape.Any);
	
	public bool UpdatePorts(GraphNode model) => false;
	
	public GraphValue Evaluate(GraphNode model, IGraphExecutionContext graph, GraphEvaluationRequest request)
		=> GraphNodeEvaluationAdapter.Evaluate(this, model, graph, request);
}
