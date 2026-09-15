using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Core;

public sealed class GraphNodeCatalog
{
	private readonly List<INodeExecutor> _executors;

	public GraphNodeCatalog(IEnumerable<INodeExecutor> executors)
	{
		_executors =
		[
			.. executors
			   .OrderBy(static e => e.GroupKey)
			   .ThenBy(static e => e.LocalizationKey)
		];
	}

	public IReadOnlyList<INodeExecutor> Executors => _executors;

	public IEnumerable<string> Groups => _executors
		.Select(static e => e.GroupKey)
		.Distinct(StringComparer.OrdinalIgnoreCase);
	
	public IEnumerable<INodeExecutor> Filter(string query, Func<string, string>? translate = null)
	{
		if (string.IsNullOrWhiteSpace(query))
			return _executors;

		return _executors.Where(e => MatchesQuery(e, query, translate));
	}

	private static bool MatchesQuery(INodeExecutor executor, string query, Func<string, string>? translate)
	{
		if (executor.Key.Contains(query, StringComparison.OrdinalIgnoreCase))
			return true;

		var suffix = executor.LocalizationKey.Contains('.')
			? executor.LocalizationKey[(executor.LocalizationKey.LastIndexOf('.') + 1)..]
			: executor.LocalizationKey;

		if (suffix.Contains(query, StringComparison.OrdinalIgnoreCase))
			return true;

		if (translate is null)
			return false;

		var translated = translate(executor.LocalizationKey + ".name");
	
		return translated.Contains(query, StringComparison.OrdinalIgnoreCase);
	}
}
