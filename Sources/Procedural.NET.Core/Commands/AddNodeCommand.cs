namespace Procedural.NET.Core.Commands;

public sealed class AddNodeCommand : IGraphCommand
{
	private readonly GraphDocument _document;
	private readonly GraphNode _graphNode;
	
	public AddNodeCommand(string name, GraphDocument document, GraphNode graphNode)
	{
		Name = name;
		
		_document = document;
		_graphNode = graphNode;
	}

	public string Name { get; }

	public void Execute() => _document.AddNode(_graphNode);
	public void Undo() => _document.RemoveNodes([_graphNode]);
}
