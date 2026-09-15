namespace Procedural.NET.Core.Commands;

public sealed class DisconnectCommand : IGraphCommand
{
	private readonly GraphDocument _document;

	private readonly Guid _inputNodeId;
	private readonly Guid _outputNodeId;
	
	private readonly string _inputPortKey;
	private readonly string _outputPortKey;

	public DisconnectCommand(
		string name, GraphDocument document,
		Guid inputNodeId, string inputPortKey,
		Guid outputNodeId, string outputPortKey)
	{
		Name = name;
		
		_document = document;
		
		_inputNodeId = inputNodeId;
		_outputNodeId = outputNodeId;
		
		_inputPortKey = inputPortKey;
		_outputPortKey = outputPortKey;
	}

	public string Name { get; }

	public void Execute() => _document.RemoveConnection(_inputNodeId, _inputPortKey, _outputNodeId, _outputPortKey);

	public void Undo() => _document.TryConnect(_outputNodeId, _outputPortKey, _inputNodeId, _inputPortKey);
}
