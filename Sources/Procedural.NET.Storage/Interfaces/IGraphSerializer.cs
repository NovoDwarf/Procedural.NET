namespace Procedural.NET.Storage.Interfaces;

public interface IGraphSerializer
{
	T Read<T>(Stream stream);
	void Write<T>(Stream stream, T value);
}
