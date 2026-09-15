
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core.Constants;

namespace Procedural.NET.Nodes.Mathematics;

public sealed class DivideNode : BinaryNode
{
	public override string Key => NodeKeys.Divide;
	public override string GroupKey => NodeGroupKeys.Math;
	public override string SubgroupKey => NodeSubgroupKeys.Arithmetic;
	
	public override ColorF Color => new(0.36f, 0.42f, 0.58f);

	protected override float Combine(float a, float b) => MathF.Abs(b) < 0.0001f ? 0f : Math.Clamp(a / b, 0f, 1f);
}
