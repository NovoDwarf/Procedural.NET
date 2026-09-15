using NovoDwarf.Primitives.Models.Float2;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Core;

public sealed class GraphNode
{
	private readonly List<GraphNodePort> _inputs;
	private readonly List<GraphNodePort> _outputs;
	
	private readonly Dictionary<string, GraphNodePort?> _inputsByKey;
	private readonly Dictionary<string, GraphNodePort?> _outputsByKey;
	
	public GraphNode(INodeExecutor executor, Float2 position)
		: this(Guid.NewGuid(), executor, position)
	{
	}

	public GraphNode(Guid id, INodeExecutor executor, Float2 position)
	{
		Id = id;
		Executor = executor;
		DeviceName = executor.LocalizationKey + ".name";
		Position = position;
		Size = new Float2(280, 160);
		Parameters = new GraphNodeParameters(executor);
		
		_inputs = [.. executor.Inputs];
		_outputs = [.. executor.Outputs];

		_inputsByKey = [];
		_outputsByKey = [];

		RebuildPortLookup();
	}

	public Guid Id { get; }
	public string DeviceName { get; set; }
	public string? CustomName { get; set; }
	public INodeExecutor Executor { get; }

	public Float2 Position { get; set; }
	public Float2 Size { get; set; }
	
	public GraphNodeParameters Parameters { get; }
	
	public string Title => $"{CustomName ?? DeviceName} #{Id}";
	
	public IReadOnlyList<GraphNodePort> Inputs => _inputs;
	public IReadOnlyList<GraphNodePort> Outputs => _outputs;
	
	public bool TryGetInputPort(string key, out GraphNodePort? port) => _inputsByKey.TryGetValue(key, out port);
	
	public bool TryGetOutputPort(string key, out GraphNodePort? port) => _outputsByKey.TryGetValue(key, out port);

	public float Get(string key) => Parameters.Get(key);

	public T Get<T>(string key) => Parameters.Get<T>(key);

	public string GetString(string key) => Parameters.GetString(key);

	public void SetString(string key, string value) => Parameters.SetString(key, value);

	public void SetParameterPinned(string key, bool pinned) => Parameters.SetParameterPinned(key, pinned);
	
	public void CopyFrom(GraphNode source)
	{
		DeviceName = source.DeviceName;
		CustomName = source.CustomName;
		Size = source.Size;
		
		Parameters.Copy(source.Parameters);
	}
	
	public bool ReplaceParameterDefinitions(IEnumerable<GraphNodeParameter> definitions)
	{
		var changed = Parameters.ReplaceParameterDefinitions(definitions);
		RebuildPortLookup();
		return changed;
	}

	public bool ReplaceInputs(IEnumerable<GraphNodePort> inputs)
	{
		if (!ReplacePorts(_inputs, inputs))
			return false;

		RebuildPortLookup();

		return true;
	}

	public bool ReplaceOutputs(IEnumerable<GraphNodePort> outputs)
	{
		if (!ReplacePorts(_outputs, outputs))
			return false;

		RebuildPortLookup();

		return true;
	}

	private void RebuildPortLookup()
	{
		_inputsByKey.Clear();
		_outputsByKey.Clear();

		foreach (var port in _inputs)
			_inputsByKey[port.Key] = port;

		foreach (var port in Parameters.ParameterInputPortsByParameter.Values)
			_inputsByKey[port.Key] = port;

		foreach (var port in _outputs)
			_outputsByKey[port.Key] = port;
	}
	
	private static bool ReplacePorts(List<GraphNodePort> target, IEnumerable<GraphNodePort> source)
	{
		var ports = source.ToList();

		if (target.SequenceEqual(ports))
			return false;

		target.Clear();
		target.AddRange(ports);
		
		return true;
	}
}
