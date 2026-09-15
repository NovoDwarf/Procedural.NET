
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core.Constants;

namespace Procedural.NET.Nodes.Mathematics;

public sealed class MaxNode : BinaryNode
{
	public override string Key => NodeKeys.Max;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Comparison;
	
	public override ColorF Color => new(0.36f, 0.42f, 0.58f);

	protected override float Combine(float a, float b) => Math.Max(a, b);
}
