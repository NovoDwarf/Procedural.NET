
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;

namespace Procedural.NET.Nodes.Utilities;

public sealed class CommentNode : BaseNode
{
	private static readonly IReadOnlyList<GraphNodePort> Default = [];
	
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters = 	
	[
		new(ParameterKeys.Text, 0f, 0f, 0f, 0f,
			Kind: ParameterKind.String, Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.Comment;
	public override string GroupKey => NodeGroupKeys.Utilities;
	
	public override ColorF Color => new(0.35f, 0.35f, 0.35f);

	public override bool SupportsPreview => false;
	public override bool AllowParameterConnections => false;

	public override IReadOnlyList<GraphNodePort> Inputs => Default;
	public override IReadOnlyList<GraphNodePort> Outputs => Default;
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;
}
