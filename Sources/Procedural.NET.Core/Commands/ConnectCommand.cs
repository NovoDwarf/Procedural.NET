namespace Procedural.NET.Core.Commands;

public sealed class ConnectCommand : IGraphCommand
{
	private readonly GraphDocument _document;
	
	private readonly Guid _inputNodeId;
	private readonly Guid _outputNodeId;
	
	private readonly string _outputPortKey;
	private readonly string _inputPortKey;
	
	private readonly GraphPort? _displaced;


	public ConnectCommand(
		string name, GraphDocument document,
		Guid outputNodeId, string outputPortKey,
		Guid inputNodeId, string inputPortKey)
	{
		Name = name;
		
		_document = document;
		_outputNodeId = outputNodeId;
		_outputPortKey = outputPortKey;
		_inputNodeId = inputNodeId;
		_inputPortKey = inputPortKey;

		if (document.TryGetInputConnection(inputNodeId, inputPortKey, out var existing))
			_displaced = existing;
	}

	public string Name { get; }
	
	public void Execute() => _document.TryConnect(_outputNodeId, _outputPortKey, _inputNodeId, _inputPortKey);

	public void Undo()
	{
		_document.RemoveConnection(_inputNodeId, _inputPortKey, _outputNodeId, _outputPortKey);

		if (_displaced is { } d)
			_document.TryConnect(d.NodeId, d.PortKey, _inputNodeId, _inputPortKey);
	}
}
