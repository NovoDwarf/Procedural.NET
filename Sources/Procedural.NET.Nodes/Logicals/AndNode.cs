
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core.Constants;

namespace Procedural.NET.Nodes.Logicals;

public sealed class AndNode : BinaryNode
{
	public override string Key => NodeKeys.And;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Logic;
	
	public override ColorF Color => new(0.36f, 0.42f, 0.58f);
	
	protected override float Combine(float a, float b) => a >= 0.5f && b >= 0.5f ? 1f : 0f;
}
