namespace Procedural.NET.Designer.ViewModels;

public sealed class InspectorParameterViewModel : ViewModelBase
{
	private string _value;
	private readonly Action<string, float>? _changed;

	public InspectorParameterViewModel(string key, string label, float value, Action<string, float>? changed = null)
	{
		Key = key;
		Label = label;
		_value = value.ToString("0.###");
		_changed = changed;
	}

	public string Key { get; }
	public string Label { get; }

	public string Value
	{
		get => _value;
		set
		{
			if (!SetProperty(ref _value, value))
				return;

			if (float.TryParse(value, out var parsed))
				_changed?.Invoke(Key, parsed);
		}
	}
}
