
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;

namespace Procedural.NET.Nodes.Mathematics;

public sealed class ClampNode : UnaryNode
{
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 	
	[
		new(ParameterKeys.Min, 0f, 1f, 0.001f, 0f, Category: ParameterCategory.Primary),
		new(ParameterKeys.Max, 0f, 1f, 0.001f, 1f, Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.Clamp;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Range;
	
	public override ColorF Color => new(0.36f, 0.42f, 0.58f);

	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;
	
	protected override float Transform(float value, Func<string, float>? param = null)
	{
		if (param == null) 
			return 0;
		
		var min = param(ParameterKeys.Min);
		var max = param(ParameterKeys.Max);
		
		return Math.Clamp(value, min, max);
	}
}
