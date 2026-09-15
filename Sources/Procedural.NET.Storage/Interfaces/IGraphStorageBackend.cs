namespace Procedural.NET.Storage.Interfaces;

public interface IGraphStorageBackend
{
	Stream OpenRead(string path);
	Stream OpenWrite(string path);
	bool Exists(string path);
}
