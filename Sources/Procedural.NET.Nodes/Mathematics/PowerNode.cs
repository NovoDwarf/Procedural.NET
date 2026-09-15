
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;

namespace Procedural.NET.Nodes.Mathematics;

public sealed class PowerNode : UnaryNode
{
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 	
	[
		new(ParameterKeys.Exponent, 0.01f, 10f, 0.01f, 2f,
			AllowGreater: true, Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.Power;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Arithmetic;
	
	public override ColorF Color => new(0.36f, 0.42f, 0.58f);

	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;
	
	protected override float Transform(float value, Func<string, float>? param = null)
	{
		if (param == null) 
			return 0;
		
		var exponent = param(ParameterKeys.Exponent);

		return Math.Clamp((float)Math.Pow(Math.Max(0f, value), exponent), 0f, 1f);
	}
}
