
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core.Constants;

namespace Procedural.NET.Nodes.Blending;

public sealed class ScreenNode : BinaryNode
{
	public override string Key => NodeKeys.Screen;
	public override string GroupKey => NodeGroupKeys.Blending;
	
	public override ColorF Color => new(0.42f, 0.36f, 0.58f);

	protected override float Combine(float a, float b) => 1f - (1f - a) * (1f - b);
}
