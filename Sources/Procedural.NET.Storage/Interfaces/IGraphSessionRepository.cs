namespace Procedural.NET.Storage.Interfaces;

public interface IGraphSessionRepository
{
	GraphSession Load(string path);
	void Save(string path, GraphSession session);
}
