
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core.Constants;

namespace Procedural.NET.Nodes.Mathematics;

public sealed class MinNode : BinaryNode
{
	public override string Key => NodeKeys.Min;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Comparison;

	public override ColorF Color => new(0.36f, 0.42f, 0.58f);

	protected override float Combine(float a, float b) => Math.Min(a, b);
}
