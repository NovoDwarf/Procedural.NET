namespace Procedural.NET.Storage;

public sealed record NodePreset(
	string Name,
	Dictionary<string, float> Parameters,
	HashSet<string> PinnedParameters,
	Dictionary<string, string>? StringParameters = null);
