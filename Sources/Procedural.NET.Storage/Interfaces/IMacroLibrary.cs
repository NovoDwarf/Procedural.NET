using Procedural.NET.Storage.Macros;

namespace Procedural.NET.Storage.Interfaces;

public interface IMacroLibrary
{
	MacroDefinition? TryLoad(string reference);
	string NormalizeReference(string path);
}
