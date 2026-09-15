
using NovoDwarf.Primitives.Models;
using NovoDwarf.Primitives.Models.Float2;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Spatial;

public sealed class RelaxPointsNode : BaseNode, IPointSetExecutor
{
	public override string Key => NodeKeys.RelaxPoints;
	public override string GroupKey => NodeGroupKeys.Utilities;
	
	public override ColorF Color => new(0.30f, 0.42f, 0.52f);

	public override IReadOnlyList<GraphNodePort> Inputs =>
	[
		new(PortKeys.Points, ValueShape.Points, PortSemantics.Position)
	];

	public override IReadOnlyList<GraphNodePort> Outputs =>
	[
		new(PortKeys.Points, ValueShape.Points, PortSemantics.Position)
	];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.Iterations, 1f, 32f, 1f, 4f, UseSlider: false, Category: ParameterCategory.Primary),
		new(ParameterKeys.Radius, 0.5f, 64f, 0.5f, 6f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Strength, 0f, 1f, 0.01f, 0.35f, Category: ParameterCategory.Advanced)
	];

	public PointSet EvaluatePoints(GraphNode model, IGraphExecutionContext graph, int width, int height)
	{
		var source = graph.RequirePointSetInput(model, PortKeys.Points, width, height);
		
		if (source.Points.Length <= 1)
			return source;

		var iterations = Math.Max(1, graph.GetParameter<int>(model, ParameterKeys.Iterations, 0.5f, 0.5f));
		var radius = Math.Max(0.01f, graph.GetParameter(model, ParameterKeys.Radius, 0.5f, 0.5f));
		var strength = Math.Clamp(graph.GetParameter(model, ParameterKeys.Strength, 0.5f, 0.5f), 0f, 1f);
		var radiusSquared = radius * radius;

		var points = source.Points.Select(point => point with { }).ToArray();

		for (var iteration = 0; iteration < iterations; iteration++)
		{
			for (var i = 0; i < points.Length; i++)
			{
				var current = points[i];
				var offset = Float2.Zero;

				for (var j = 0; j < points.Length; j++)
				{
					if (i == j)
						continue;

					var other = points[j];
					var delta = current.Position - other.Position;
					var distanceSquared = delta.X * delta.X + delta.Y * delta.Y;
					if (distanceSquared <= 0.0001f || distanceSquared >= radiusSquared)
						continue;

					var distance = MathF.Sqrt(distanceSquared);
					var push = (radius - distance) / radius;
					offset += delta / distance * push;
				}

				if (offset == Float2.Zero)
					continue;

				var position = current.Position + offset * strength;
				points[i] = current with
				{
					Position = new Float2(
						Math.Clamp(position.X, 0f, width - 1f),
						Math.Clamp(position.Y, 0f, height - 1f))
				};
			}
		}

		return new PointSet(width, height, points);
	}
}
