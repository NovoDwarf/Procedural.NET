using Avalonia.Media;

namespace Procedural.NET.Designer.ViewModels;

public sealed class CatalogNodeViewModel
{
	public CatalogNodeViewModel(string executorKey, string name, string group, string subgroup, Color color)
	{
		ExecutorKey = executorKey;
		Name = name;
		Group = group;
		Subgroup = subgroup;
		ColorBrush = new SolidColorBrush(color);
	}

	public string ExecutorKey { get; }
	public string Name { get; }
	public string Group { get; }
	public string Subgroup { get; }
	public IBrush ColorBrush { get; }
}
