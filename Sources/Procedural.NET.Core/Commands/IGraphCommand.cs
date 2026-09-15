namespace Procedural.NET.Core.Commands;

public interface IGraphCommand
{
	public string Name { get; }
	public void Execute();
	public void Undo();
}
