
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;

namespace Procedural.NET.Nodes.Macros;

public sealed class MacroInfoNode : BaseNode
{
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters =
	[
		new(ParameterKeys.DisplayName, 0f, 0f, 0f, 0f,
			Kind: ParameterKind.String, Category: ParameterCategory.Primary),
		new(ParameterKeys.DescriptionText, 0f, 0f, 0f, 0f,
			Kind: ParameterKind.String, Category: ParameterCategory.Primary),
		new(ParameterKeys.Author, 0f, 0f, 0f, 0f,
			Kind: ParameterKind.String, Category: ParameterCategory.Primary),
		new(ParameterKeys.Tags, 0f, 0f, 0f, 0f,
			Kind: ParameterKind.String, Category: ParameterCategory.Advanced)
	];
	
	public override string Key => NodeKeys.MacroInfo;
	public override string GroupKey => NodeGroupKeys.Utilities;
	public override string SubgroupKey => NodeSubgroupKeys.Macro;
	
	public override ColorF Color => new(0.38f, 0.34f, 0.22f);
	
	public override bool SupportsPreview => false;
	public override bool AllowParameterConnections => false;

	public override IReadOnlyList<GraphNodePort> Inputs => [];
	public override IReadOnlyList<GraphNodePort> Outputs => [];
	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;

}
