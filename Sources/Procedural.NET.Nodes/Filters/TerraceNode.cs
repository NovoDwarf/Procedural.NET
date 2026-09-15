
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Filters;

public sealed class TerraceNode : UnaryNode, ISampledExecutor, IFieldExecutor
{
	public override string Key => NodeKeys.Terrace;
	public override string GroupKey => NodeGroupKeys.Filters;
	
	public override ColorF Color => new(0.36f, 0.50f, 0.44f);
	
	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Steps, 2f, 64f, 1f, 8f,
			UseSlider: false, Category: ParameterCategory.Primary),
		new(ParameterKeys.Smoothness, 0f, 1f, 0.01f, 0f,
			Category: ParameterCategory.Primary)
	];

	protected override float Transform(float value, Func<string, float>? param = null)
	{
		if (param is null)
			return value;

		return Terrace(value, param(ParameterKeys.Steps), param(ParameterKeys.Smoothness));
	}

	public float EvaluatePoint(GraphNode model, IGraphExecutionContext graph, EvaluationContext context)
	{
		var a = graph.RequireScalarInput(model, PortKeys.A, context);
		var steps = graph.GetParameter(model, ParameterKeys.Steps, context);
		var smoothness = graph.GetParameter(model, ParameterKeys.Smoothness, context);
		return Terrace(a, steps, smoothness);
	}

	public Field EvaluateField(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var aField = graph.RequireFieldInput(model, PortKeys.A, width, height);
		var result = new Field(width, height);
		
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var u = (float)x / width;
			var v = (float)y / height;
			var steps = graph.GetParameter(model, ParameterKeys.Steps, u, v);
			var smoothness = graph.GetParameter(model, ParameterKeys.Smoothness, u, v);
			result[x, y] = Terrace(aField[x, y], steps, smoothness);
		}
		
		return result;
	}

	private static float Terrace(float value, float steps, float smoothness)
	{
		var s = Math.Max(1f, steps);
		var scaled = value * s;
		var floored = MathF.Floor(scaled);
		var fract = scaled - floored;
		var smooth = fract * fract * (3f - 2f * fract);
		var quantized = (floored + Math.Clamp(smoothness > 0f ? smooth * smoothness + fract * (1f - smoothness) : 0f, 0f, 1f)) / s;
		
		return Math.Clamp(quantized, 0f, 1f);
	}
}
