using System.Text.Json;

namespace Procedural.NET.Nodes.Spatial.Layout;

public sealed record LayoutDocument
{
	private static readonly JsonSerializerOptions Options = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public List<LayoutShape> Shapes { get; init; } = [];

	public static LayoutDocument Default() => new()
	{
		Shapes =
		[
			new LayoutShape
			{
				Kind = LayoutShapeKind.Circle,
				X = 0.5f,
				Y = 0.5f,
				Width = 0.72f,
				Height = 0.72f,
				Strength = 1f
			}
		]
	};

	public static LayoutDocument Parse(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return Default();

		try
		{
			return JsonSerializer.Deserialize<LayoutDocument>(json, Options) ?? Default();
		}
		catch (JsonException)
		{
			return Default();
		}
	}

	public string ToJson() => JsonSerializer.Serialize(this, Options);
}
