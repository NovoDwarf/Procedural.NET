namespace Procedural.NET.Designer.ViewModels;

public sealed class GraphConnectionViewModel
{
	public GraphConnectionViewModel(GraphNodeViewModel output, string outputPortKey, GraphNodeViewModel input, string inputPortKey)
	{
		Output = output;
		OutputPortKey = outputPortKey;
		Input = input;
		InputPortKey = inputPortKey;
	}

	public GraphNodeViewModel Output { get; }
	public string OutputPortKey { get; }
	public GraphNodeViewModel Input { get; }
	public string InputPortKey { get; }
}
