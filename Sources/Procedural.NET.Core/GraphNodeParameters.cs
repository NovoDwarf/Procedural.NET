using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution.Interfaces;
using Procedural.NET.Core.Utilities;

namespace Procedural.NET.Core;

public sealed class GraphNodeParameters
{
	private readonly List<GraphNodeParameter> _parameterDefinitions;

	private readonly Dictionary<string, float> _parameters;
	private readonly Dictionary<string, string> _stringParameters = [];
	private readonly Dictionary<string, GraphNodePort> _parameterInputPortsByParameter;
	
	private readonly HashSet<string> _pinnedParameters = [];
	
	public GraphNodeParameters(INodeExecutor executor)
	{
		_parameterDefinitions = [.. executor.Parameters];
		_parameterInputPortsByParameter = _parameterDefinitions.ToDictionary(
			static p => p.Key,
			static p => new GraphNodePort(GraphRules.ParameterPortKey(p.Key), ParameterPortShape(p), CustomLocKey: p.CustomLocKey));
		
		_parameters = _parameterDefinitions.ToDictionary(static p => p.Key, static p => p.DefaultValue);
	}
	
	public IReadOnlyDictionary<string, float> Parameters => _parameters;
	public IReadOnlyDictionary<string, string> StringParameters => _stringParameters;
	public IReadOnlyDictionary<string, GraphNodePort> ParameterInputPortsByParameter => _parameterInputPortsByParameter;
	public IReadOnlySet<string> PinnedParameters => _pinnedParameters;
	public IReadOnlyList<GraphNodeParameter> ParameterDefinitions => _parameterDefinitions;
	
	public float Get(string key)
	{
		return _parameters.TryGetValue(key, out var value)
			? value
			: throw new KeyNotFoundException($"Node does not define parameter [{key}].");
	}

	public T Get<T>(string key)
	{
		var value = Get(key);

		return BasicParameterConverter.Convert<T>(value, key);
	}

	public void Set(string key, float value)
	{
		if (!_parameters.ContainsKey(key))
			throw new KeyNotFoundException($"Node does not define parameter [{key}].");

		_parameters[key] = value;
	}
	
	public string GetString(string key) 
		=> _stringParameters.TryGetValue(key, out var value) ? value : string.Empty;

	public void SetString(string key, string value) 
		=> _stringParameters[key] = value;

	public bool IsParameterPinned(string key) 
		=> _pinnedParameters.Contains(key);

	public void SetParameterPinned(string key, bool pinned)
	{
		if (!_parameters.ContainsKey(key))
			throw new KeyNotFoundException($"Node does not define parameter [{key}].");

		if (pinned)
			_pinnedParameters.Add(key);
		else
			_pinnedParameters.Remove(key);
	}
	
	public bool ReplaceParameterDefinitions(IEnumerable<GraphNodeParameter> definitions)
	{
		var next = definitions.ToList();
		if (_parameterDefinitions.SequenceEqual(next))
			return false;

		var oldValues = _parameters.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		var oldPins = _pinnedParameters.ToHashSet(StringComparer.Ordinal);

		_parameterDefinitions.Clear();
		_parameterDefinitions.AddRange(next);

		_parameters.Clear();
		_parameterInputPortsByParameter.Clear();
		
		foreach (var parameter in _parameterDefinitions)
		{
			_parameters[parameter.Key] = oldValues.TryGetValue(parameter.Key, out var value)
				? value
				: parameter.DefaultValue;
			
			_parameterInputPortsByParameter[parameter.Key] =
				new GraphNodePort(GraphRules.ParameterPortKey(parameter.Key), ParameterPortShape(parameter), CustomLocKey: parameter.CustomLocKey);
		}

		_pinnedParameters.Clear();
		
		foreach (var key in oldPins.Where(key => _parameters.ContainsKey(key))) 
			_pinnedParameters.Add(key);

		return true;
	}

	public void Copy(GraphNodeParameters source)
	{
		ReplaceParameterDefinitions(source.ParameterDefinitions);

		foreach (var (key, value) in source.Parameters)
		{
			if (_parameters.ContainsKey(key))
				_parameters[key] = value;
		}

		_stringParameters.Clear();
		
		foreach (var (key, value) in source.StringParameters)
			_stringParameters[key] = value;

		_pinnedParameters.Clear();
		
		foreach (var key in source.PinnedParameters)
			if (_parameters.ContainsKey(key))
				_pinnedParameters.Add(key);
	}
	
	private static ValueShape ParameterPortShape(GraphNodeParameter parameter)
	{
		return parameter.Kind == ParameterKind.Checkbox
			? ValueShape.Boolean
			: ValueShape.Any;
	}
}