
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes;

public abstract class BaseNode : INodeExecutor
{
	public abstract string Key { get; }
	public virtual string LocalizationKey => Key;
	public abstract string GroupKey { get; }
	public virtual string SubgroupKey => string.Empty;

	public abstract ColorF Color { get; }

	public abstract IReadOnlyList<GraphNodePort> Inputs { get; }
	public abstract IReadOnlyList<GraphNodePort> Outputs { get; }
	public virtual IReadOnlyList<GraphNodeParameter> Parameters => [];

	public virtual bool SupportsPreview => true;
	public virtual bool AllowParameterConnections => true;
}