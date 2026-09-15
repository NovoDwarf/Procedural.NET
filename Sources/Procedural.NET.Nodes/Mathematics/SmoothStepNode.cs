
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;

namespace Procedural.NET.Nodes.Mathematics;

public sealed class SmoothStepNode : UnaryNode
{
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 	
	[
		new(ParameterKeys.Edge0, 0f, 1f, 0.001f, 0f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Edge1, 0f, 1f, 0.001f, 1f, Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.SmoothStep;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Range;

	public override ColorF Color => new(0.36f, 0.42f, 0.58f);
	
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	protected override float Transform(float value, Func<string, float>? param = null)
	{
		if (param == null)
			return float.NaN;

		var edge0 = param(ParameterKeys.Edge0);
		var edge1 = param(ParameterKeys.Edge1);
		
		return Smoothstep(edge0, edge1, value); 
	}
	
	private static float Smoothstep(float edge0, float edge1, float x)
	{
		if (Math.Abs(edge1 - edge0) < float.Epsilon)
			return 0f;
		
		var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
		
		return t * t * (3f - 2f * t);
	}
}
