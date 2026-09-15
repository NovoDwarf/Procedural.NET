
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;

namespace Procedural.NET.Nodes.Macros;

public sealed class MacroViewNode : BaseNode
{
	private const string DefaultPortName = "Preview";

	public override string Key => NodeKeys.MacroView;
	public override string GroupKey => NodeGroupKeys.Utilities;
	public override string SubgroupKey => NodeSubgroupKeys.Macro;
	
	public override ColorF Color => new(0.36f, 0.50f, 0.62f);
	
	public override bool SupportsPreview => false;
	public override bool AllowParameterConnections => false;

	public override IReadOnlyList<GraphNodePort> Inputs => [];
	public override IReadOnlyList<GraphNodePort> Outputs => [];

	public override IReadOnlyList<GraphNodeParameter> Parameters =>
	[
		new(ParameterKeys.DisplayName, 0f, 0f, 0f, 0f,
			Kind: ParameterKind.String, Category: ParameterCategory.Primary),
		new(ParameterKeys.RuntimeShape, 0f, 7f, 1f, 3f,
			UseSlider: false, Kind: ParameterKind.Option, Options: MacroNodeContracts.RuntimeShapeOptions, Category: ParameterCategory.Primary),
		new(ParameterKeys.RuntimeSemantics, 0f, 9f, 1f, 1f,
			UseSlider: false, Kind: ParameterKind.Option, Options: MacroNodeContracts.RuntimeSemanticsOptions, Category: ParameterCategory.Primary)
	];

	public bool UpdatePorts(GraphNode model)
	{
		var label = string.IsNullOrWhiteSpace(model.GetString(ParameterKeys.DisplayName))
			? DefaultPortName
			: model.GetString(ParameterKeys.DisplayName);
		
		var shape = MacroNodeContracts.ResolveRuntimeShape(model);
		var semantics = MacroNodeContracts.ResolveRuntimeSemantics(model);
		
		return model.ReplaceInputs([new GraphNodePort(PortKeys.Source, shape, semantics, label)]);
	}
}
