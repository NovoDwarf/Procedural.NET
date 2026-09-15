using System.Collections.Immutable;
using Procedural.NET.Core.Execution;

namespace Procedural.NET.Nodes.Macros;

internal sealed class MacroEvaluationScope : IDisposable
{
	private static readonly AsyncLocal<ImmutableStack<MacroEvaluationBindings>> CurrentStack = new();
	private static readonly AsyncLocal<ImmutableHashSet<string>> ActiveGraphs = new();

	private readonly string? _graphPath;

	private MacroEvaluationScope(string? graphPath)
	{
		_graphPath = graphPath;
	}

	public static MacroEvaluationScope Push(MacroEvaluationBindings bindings, string graphPath)
	{
		var active = ActiveGraphs.Value ?? ImmutableHashSet<string>.Empty;

		if (!string.IsNullOrEmpty(graphPath) && active.Contains(graphPath))
			throw new InvalidOperationException($"Macro cycle detected: '{graphPath}' is already in the evaluation stack.");

		CurrentStack.Value = (CurrentStack.Value ?? ImmutableStack<MacroEvaluationBindings>.Empty).Push(bindings);
		ActiveGraphs.Value = string.IsNullOrEmpty(graphPath) ? active : active.Add(graphPath);
		return new MacroEvaluationScope(graphPath);
	}

	public static bool TryGetValue(Guid nodeId, out GraphValue value)
	{
		var stack = CurrentStack.Value;
		while (stack is { IsEmpty: false })
		{
			if (stack.Peek().Values.TryGetValue(nodeId, out value))
				return true;

			stack = stack.Pop();
		}

		value = null!;
		return false;
	}

	public static bool TryGetValue(string key, out GraphValue value)
	{
		var stack = CurrentStack.Value;
		while (stack is { IsEmpty: false })
		{
			if (stack.Peek().PortValues.TryGetValue(key, out value))
				return true;

			stack = stack.Pop();
		}

		value = null!;
		return false;
	}

	public void Dispose()
	{
		var stack = CurrentStack.Value;
		
		if (stack is { IsEmpty: false })
			CurrentStack.Value = stack.Pop();

		if (_graphPath is null) 
			return;
		
		var active = ActiveGraphs.Value ?? ImmutableHashSet<string>.Empty;
		ActiveGraphs.Value = active.Remove(_graphPath);
	}
}

internal sealed record MacroEvaluationBindings(
	IReadOnlyDictionary<Guid, GraphValue> Values,
	IReadOnlyDictionary<string, GraphValue> PortValues);
