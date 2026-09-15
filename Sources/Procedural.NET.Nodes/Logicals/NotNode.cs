
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core.Constants;

namespace Procedural.NET.Nodes.Logicals;

public sealed class NotNode : UnaryNode
{
	public override string Key => NodeKeys.Not;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Logic;
	
	public override ColorF Color => new(0.36f, 0.42f, 0.58f);
	
	protected override float Transform(float value, Func<string, float>? param = null) => value >= 0.5f ? 0f : 1f;
}
