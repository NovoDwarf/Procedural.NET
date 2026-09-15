namespace Procedural.NET.Storage.Interfaces;

public interface IGraphNodePresetRepository
{
	IReadOnlyList<NodePreset> GetPresets(string executorKey);
	void SavePreset(string executorKey, NodePreset preset);
	bool DeletePreset(string executorKey, string presetName);
}
