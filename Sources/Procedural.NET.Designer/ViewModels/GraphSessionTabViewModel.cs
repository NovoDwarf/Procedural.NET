using Procedural.NET.Core;

namespace Procedural.NET.Designer.ViewModels;

public sealed class GraphSessionTabViewModel
{
	public GraphSessionTabViewModel(string name, GraphDocument document)
	{
		Name = name;
		Document = document;
	}

	public string Name { get; }
	public GraphDocument Document { get; }
}
