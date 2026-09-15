using System.Collections.Concurrent;
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core.Caching;

namespace Procedural.NET.Core.Execution;

public sealed class GraphDocumentSnapshot : IGraphRuntimeDocument
{
	private readonly Dictionary<Guid, GraphNode> _nodesById;
	private readonly GraphConnection _graphConnections;
	
	private readonly ConcurrentDictionary<FieldCacheKey, Field> _fieldCache = new();
	private readonly ConcurrentDictionary<StructuredCacheKey, PointSet> _pointCache = new();
	private readonly ConcurrentDictionary<StructuredCacheKey, PathSet> _pathCache = new();
	
	public GraphMetadata Metadata { get; }

	private GraphDocumentSnapshot(Dictionary<Guid, GraphNode> nodesById, GraphConnection graphConnections, GraphMetadata metadata)
	{
		_nodesById   = nodesById;
		_graphConnections = graphConnections;
		Metadata = metadata;
	}

	public static GraphDocumentSnapshot Capture(GraphDocument source)
	{
		var nodes = new Dictionary<Guid, GraphNode>(source.Nodes.Count);

		foreach (var node in source.Nodes)
		{
			var clone = new GraphNode(node.Id, node.Executor, node.Position);
			
			clone.CopyFrom(node);
			clone.ReplaceInputs(node.Inputs);
			clone.ReplaceOutputs(node.Outputs);
			
			nodes[node.Id] = clone;
		}

		return new GraphDocumentSnapshot(nodes, source.CaptureConnections(), CloneMetadata(source.Metadata));
	}

	public GraphNode GetNode(Guid id) => _nodesById[id];

	public bool TryGetInputConnection(Guid inputNodeId, string inputPortKey, out GraphPort output)
		=> _graphConnections.TryGetOutput(inputNodeId, inputPortKey, out output);

	public bool TryGetCachedField(Guid outputNodeId, string outputPortKey, int width, int height, out Field buffer)
		=> _fieldCache.TryGetValue(new FieldCacheKey(outputNodeId, outputPortKey, width, height), out buffer!);

	public void SetCachedField(Guid outputNodeId, string outputPortKey, int width, int height, Field buffer)
		=> _fieldCache[new FieldCacheKey(outputNodeId, outputPortKey, width, height)] = buffer;

	public bool TryGetCachedPointSet(Guid outputNodeId, string outputPortKey, int width, int height, out PointSet buffer)
		=> _pointCache.TryGetValue(new StructuredCacheKey(outputNodeId, outputPortKey, width, height), out buffer!);

	public void SetCachedPointSet(Guid outputNodeId, string outputPortKey, int width, int height, PointSet buffer)
		=> _pointCache[new StructuredCacheKey(outputNodeId, outputPortKey, width, height)] = buffer;

	public bool TryGetCachedPathSet(Guid outputNodeId, string outputPortKey, int width, int height, out PathSet buffer)
		=> _pathCache.TryGetValue(new StructuredCacheKey(outputNodeId, outputPortKey, width, height), out buffer!);

	public void SetCachedPathSet(Guid outputNodeId, string outputPortKey, int width, int height, PathSet buffer)
		=> _pathCache[new StructuredCacheKey(outputNodeId, outputPortKey, width, height)] = buffer;

	private static GraphMetadata CloneMetadata(GraphMetadata source)
	{
		return new GraphMetadata
		{
			Name = source.Name,
			Description = source.Description,
			Author = source.Author,
			CreatedUtc = source.CreatedUtc,
			ModifiedUtc = source.ModifiedUtc,
			Seed = source.Seed,
			GeneratorVersion = source.GeneratorVersion,
			AreaWidth = source.AreaWidth,
			AreaHeight = source.AreaHeight,
			OriginX = source.OriginX,
			OriginY = source.OriginY,
			AspectRatio = source.AspectRatio,
			BuildResolutionPower = source.BuildResolutionPower,
			BuildResolutionMode = source.BuildResolutionMode,
			SeaLevel = source.SeaLevel,
			ColorPreset = source.ColorPreset,
			Units = source.Units,
			RescalingMode = source.RescalingMode,
			Tags = new Dictionary<string, string>(source.Tags, StringComparer.OrdinalIgnoreCase)
		};
	}
}
