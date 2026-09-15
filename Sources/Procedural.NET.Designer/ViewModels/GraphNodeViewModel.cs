using Avalonia;
using Avalonia.Media;
using NovoDwarf.Primitives.Models.Float2;
using Procedural.NET.Core;
using Procedural.NET.Core.Enums;

namespace Procedural.NET.Designer.ViewModels;

public sealed class GraphNodeViewModel : ViewModelBase
{
	private static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["generator.node_group.blending"] = "Смешивание",
		["generator.node_group.constants"] = "Константы",
		["generator.node_group.display"] = "Отображение",
		["generator.node_group.export"] = "Экспорт",
		["generator.node_group.filters"] = "Фильтры",
		["generator.node_group.file"] = "Файлы",
		["generator.node_group.generators"] = "Генераторы",
		["generator.node_group.logic"] = "Логика",
		["generator.node_group.math"] = "Математика",
		["generator.node_group.nature"] = "Природа",
		["generator.node_group.utilities"] = "Утилиты",
		["generator.node_subgroup.noise"] = "Шум",
		["generator.node_subgroup.color"] = "Цвет",
		["generator.node_subgroup.constants"] = "Константы",
		["generator.node_subgroup.layout"] = "Раскладка",
		["generator.node_subgroup.selectors"] = "Селекторы",
		["generator.node_subgroup.arithmetic"] = "Арифметика",
		["generator.noise.perlin"] = "Перлин",
		["generator.noise.value"] = "Значение",
		["generator.noise.voronoi"] = "Вороной",
		["generator.gradient"] = "Градиент",
		["generator.color_ramp"] = "Цветовая шкала",
		["generator.display.density"] = "Плотность",
		["frequency"] = "Частота",
		["octaves"] = "Октавы",
		["seed"] = "Seed",
		["gain"] = "Усиление",
		["lacunarity"] = "Лакунарность",
		["value"] = "Значение",
		["source"] = "Источник",
		["mask"] = "Маска",
		["color"] = "Цвет",
		["bitmap"] = "Текстура",
		["preview"] = "Превью",
		["c"] = "Высота",
		["a"] = "A",
		["b"] = "B"
	};

	private bool _isSelected;

	public GraphNodeViewModel(GraphNode model, string displayName, Color color)
	{
		Model = model;
		DisplayName = displayName;
		ColorBrush = new SolidColorBrush(color);
	}

	public GraphNode Model { get; }
	public Guid Id => Model.Id;
	public string DisplayName { get; }
	public IBrush ColorBrush { get; }

	public double X
	{
		get => Model.Position.X;
		set
		{
			if (Math.Abs(Model.Position.X - value) < 0.001)
				return;

			Model.Position = new Float2((float)value, Model.Position.Y);
			OnPropertyChanged();
			OnPropertyChanged(nameof(Position));
		}
	}

	public double Y
	{
		get => Model.Position.Y;
		set
		{
			if (Math.Abs(Model.Position.Y - value) < 0.001)
				return;

			Model.Position = new Float2(Model.Position.X, (float)value);
			OnPropertyChanged();
			OnPropertyChanged(nameof(Position));
		}
	}

	public Point Position => new(X, Y);
	
	public IReadOnlyList<GraphNodePort> Inputs => Model.Inputs;
	public IReadOnlyList<GraphNodePort> Outputs => Model.Outputs;
	public IReadOnlyDictionary<string, float> Parameters => Model.Parameters.Parameters;
	
	public string StatusText => IsSelected ? "Выбран" : "Готов";
	public string PreviewMode => Outputs.Any(static port => port.Shape is ValueShape.Volume or ValueShape.Points or ValueShape.Paths) ? "3D" : "2D";
	public bool HasPreview => Outputs.Count > 0;
	
	public int PreviewVersion { get; private set; }

	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (!SetProperty(ref _isSelected, value))
				return;

			OnPropertyChanged(nameof(StatusText));
		}
	}

	public static string Label(GraphNodePort port)
	{
		return Label(port.CustomLocKey ?? port.Key);
	}

	public static string Label(string key)
	{
		if (Labels.TryGetValue(key, out var label))
			return label;

		var suffix = key.Contains('.')
			? key[(key.LastIndexOf('.') + 1)..]
			: key;

		return Labels.TryGetValue(suffix, out var suffixLabel)
			? suffixLabel
			: suffix.Replace('_', ' ');
	}

	public void RefreshPreview()
	{
		PreviewVersion++;
		OnPropertyChanged(nameof(PreviewVersion));
	}
}
