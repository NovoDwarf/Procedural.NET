
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core.Constants;

namespace Procedural.NET.Nodes.Mathematics;

public sealed class AbsNode : UnaryNode
{
	public override string Key => NodeKeys.Abs;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Rounding;
	
	public override ColorF Color => new(0.36f, 0.42f, 0.58f);

	protected override float Transform(float value, Func<string, float>? param = null) => Math.Abs(value);
}
