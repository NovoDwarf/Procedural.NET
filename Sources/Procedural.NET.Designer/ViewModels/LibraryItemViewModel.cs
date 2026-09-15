using System.Collections.ObjectModel;
using Avalonia.Media;

namespace Procedural.NET.Designer.ViewModels;

public sealed class LibraryItemViewModel
{
	public LibraryItemViewModel(string name, LibraryItemKind kind, CatalogNodeViewModel? catalogItem = null, Color? color = null)
	{
		Name = name;
		Kind = kind;
		CatalogItem = catalogItem;
		ColorBrush = new SolidColorBrush(color ?? Colors.Transparent);
	}

	public string Name { get; }
	public LibraryItemKind Kind { get; }
	public CatalogNodeViewModel? CatalogItem { get; }
	public IBrush ColorBrush { get; }
	public ObservableCollection<LibraryItemViewModel> Children { get; } = [];
	public bool HasColor => CatalogItem is not null;
}
