namespace Procedural.NET.Core.Commands;

public sealed class CompositeCommand : IGraphCommand
{
	private readonly IReadOnlyList<IGraphCommand> _commands;
	
	public CompositeCommand(string name, IReadOnlyList<IGraphCommand> commands)
	{
		Name = name;
		
		_commands = commands;
	}

	public string Name { get; }

	public void Execute()
	{
		foreach (var cmd in _commands)
			cmd.Execute();
	}

	public void Undo()
	{
		for (var i = _commands.Count - 1; i >= 0; i--)
			_commands[i].Undo();
	}
}
