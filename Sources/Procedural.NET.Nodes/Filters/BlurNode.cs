
using NovoDwarf.Primitives.Models;
using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution.Interfaces;

namespace Procedural.NET.Nodes.Filters;

public sealed class BlurNode : UnaryNode, ISampledExecutor, IFieldExecutor
{
	private static readonly IReadOnlyList<GraphNodeParameter> DefaultParameters =
	[
		new(ParameterKeys.Radius, 1f, 32f, 1f, 2f,
			UseSlider: false, Category: ParameterCategory.Primary)
	];
	
	public override string Key => NodeKeys.Blur;
	public override string GroupKey => NodeGroupKeys.Filters;
	
	public override ColorF Color => new(0.36f, 0.50f, 0.44f);

	public override IReadOnlyList<GraphNodeParameter> Parameters => DefaultParameters;
	
	protected override float Transform(float value, Func<string, float>? param = null)
	{
		if (param == null)
			return float.NaN;

		var radius = param(ParameterKeys.Radius);

		return 1; // TODO: fix
	}

	private static Field BoxBlur(Field source, int width, int height, int radius)
	{
		using var temp = Field.Rent(width, height);
		
		var result = new Field(width, height);
		var invSize = 1f / (2 * radius + 1);

		for (var y = 0; y < height; y++)
		{
			var sum = 0f;
			
			for (var kx = -radius; kx <= radius; kx++)
				sum += source[Math.Clamp(kx, 0, width - 1), y];
			
			temp[0, y] = sum * invSize;

			for (var x = 1; x < width; x++)
			{
				sum -= source[Math.Max(x - radius - 1, 0), y];
				sum += source[Math.Min(x + radius, width - 1), y];
				temp[x, y] = sum * invSize;
			}
		}

		for (var x = 0; x < width; x++)
		{
			var sum = 0f;
			for (var ky = -radius; ky <= radius; ky++)
				sum += temp[x, Math.Clamp(ky, 0, height - 1)];
			
			result[x, 0] = sum * invSize;

			for (var y = 1; y < height; y++)
			{
				sum -= temp[x, Math.Max(y - radius - 1, 0)];
				sum += temp[x, Math.Min(y + radius, height - 1)];
				result[x, y] = sum * invSize;
			}
		}

		return result;
	}
}
