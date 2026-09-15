
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;

namespace Procedural.NET.Nodes.Mathematics;

public sealed class RemapNode : UnaryNode
{
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 	
	[
		new(ParameterKeys.InMin, -99999f, 99999f, 0.001f, 0f,
			AllowGreater: true, AllowLesser: true, Category: ParameterCategory.Primary),
		new(ParameterKeys.InMax, -99999f, 99999f, 0.001f, 1f,
			AllowGreater: true, AllowLesser: true, Category: ParameterCategory.Primary),
		new(ParameterKeys.OutMin, -99999f, 99999f, 0.001f, 0f,
			AllowGreater: true, AllowLesser: true, Category: ParameterCategory.Primary),
		new(ParameterKeys.OutMax, -99999f, 99999f, 0.001f, 1f,
			AllowGreater: true, AllowLesser: true, Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.Remap;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Range;
	
	public override ColorF Color => new(0.36f, 0.42f, 0.58f);

	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

	protected override float Transform(float value, Func<string, float>? param = null)
	{
		if (param == null) 
			return 0;
		
		var inMin = param(ParameterKeys.InMin);
		var inMax = param(ParameterKeys.InMax);
		var outMin = param(ParameterKeys.OutMin);
		var outMax = param(ParameterKeys.OutMax);
		
		return Remap(value, inMin, inMax, outMin, outMax);
	}
	
	private static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
	{
		if (Math.Abs(inMax - inMin) < float.Epsilon) 
			return outMin;
		
		var t = (value - inMin) / (inMax - inMin);
		
		return outMin + t * (outMax - outMin);
	}
}
