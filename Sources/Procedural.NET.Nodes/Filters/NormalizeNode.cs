
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Filters;

public sealed class NormalizeNode : UnaryNode, ISampledExecutor, IFieldExecutor
{
	public override string Key => NodeKeys.Normalize;
	public override string GroupKey => NodeGroupKeys.Filters;
	
	public override ColorF Color => new(0.36f, 0.50f, 0.44f);

	protected override float Transform(float value, Func<string, float>? param = null) => value;

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var source = graph.RequireFieldInput(model, PortKeys.A, width, height);
		var min = float.MaxValue;
		var max = float.MinValue;

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var value = source[x, y];
			if (value < min) min = value;
			if (value > max) max = value;
		}

		var result = new Field(width, height);
		var range = max - min;
		if (range < float.Epsilon)
			return result;

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			result[x, y] = (source[x, y] - min) / range;

		return result;
	}
}
