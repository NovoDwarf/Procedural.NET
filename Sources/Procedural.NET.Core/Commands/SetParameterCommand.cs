namespace Procedural.NET.Core.Commands;

public sealed class SetParameterCommand : IGraphCommand
{
	private readonly GraphDocument _document;
	private readonly GraphNode _graphNode;
	
	private readonly string _key;
	private readonly float _before;
	private readonly float _after;

	public SetParameterCommand(
		string name, GraphDocument document, GraphNode graphNode,
		string key, float before, float after)
	{
		Name = name;
		
		_document = document;
		_graphNode = graphNode;
		_key = key;
		_before = before;
		_after = after;
	}

	public string Name { get; }

	public void Execute()
	{
		_graphNode.Parameters.Set(_key, _after);  
		_document.NotifyNodeChanged(_graphNode.Id);
	}

	public void Undo()
	{
		_graphNode.Parameters.Set(_key, _before); 
		_document.NotifyNodeChanged(_graphNode.Id);
	}
}
