using NovoDwarf.Primitives.Models;

using Procedural.NET.Core;
using Procedural.NET.Core.Constants;
using Procedural.NET.Core.Enums;
using Procedural.NET.Core.Execution;
using Procedural.NET.Storage.Macros;

namespace Procedural.NET.Nodes.Macros;

public static class MacroNodeContracts
{
	public const string BaseParameterSection = "generator.designer.parameters";
	public const string MacroParameterSection = "generator.section.macro_parameters";

	public static IReadOnlyList<string> RuntimeShapeOptions =>
	[
		OptionLocalizationKeys.MacroShapeFloat,
		OptionLocalizationKeys.MacroShapeInteger,
		OptionLocalizationKeys.MacroShapeBoolean,
		OptionLocalizationKeys.MacroShapeField,
		OptionLocalizationKeys.MacroShapeBitmap,
		OptionLocalizationKeys.MacroShapeColor,
		OptionLocalizationKeys.MacroShapePoints,
		OptionLocalizationKeys.MacroShapePaths
	];

	public static IReadOnlyList<string> RuntimeSemanticsOptions =>
	[
		OptionLocalizationKeys.MacroSemanticsGeneric,
		OptionLocalizationKeys.MacroSemanticsTerrain,
		OptionLocalizationKeys.MacroSemanticsWater,
		OptionLocalizationKeys.MacroSemanticsMask,
		OptionLocalizationKeys.MacroSemanticsTexture,
		OptionLocalizationKeys.MacroSemanticsSlope,
		OptionLocalizationKeys.MacroSemanticsCurvature,
		OptionLocalizationKeys.MacroSemanticsErosion,
		OptionLocalizationKeys.MacroSemanticsDensity,
		OptionLocalizationKeys.MacroSemanticsColor
	];

	public static IReadOnlyList<string> ParameterTypeOptions =>
	[
		OptionLocalizationKeys.MacroParamFloat,
		OptionLocalizationKeys.MacroParamInteger,
		OptionLocalizationKeys.MacroParamBoolean
	];

	public static ValueShape ResolveRuntimeShape(GraphNode model) =>
		MacroRuntimeMapping.ShapeFromIndex(model.Get<int>(ParameterKeys.RuntimeShape));

	public static ValueShape ResolveRuntimeShape(int index) =>
		MacroRuntimeMapping.ShapeFromIndex(index);

	public static PortSemantics ResolveRuntimeSemantics(GraphNode model) =>
		MacroRuntimeMapping.SemanticsFromIndex(model.Get<int>(ParameterKeys.RuntimeSemantics));

	public static PortSemantics ResolveRuntimeSemantics(int index) =>
		MacroRuntimeMapping.SemanticsFromIndex(index);

	public static ValueShape ResolveParameterShape(GraphNode model) =>
		ResolveParameterShape(model.Get<int>(ParameterKeys.MacroParameterType));

	public static ValueShape ResolveParameterShape(int index) => index switch
	{
		1 => ValueShape.Integer,
		2 => ValueShape.Boolean,
		_ => ValueShape.Float
	};

	public static string InputPortKey(Guid nodeId) => $"macro_input_{nodeId}";
	public static string OutputPortKey(Guid nodeId) => $"macro_output_{nodeId}";
	public static string PreviewPortKey(Guid nodeId) => $"macro_preview_{nodeId}";
	public static string ParameterKey(Guid nodeId, string portKey) => $"macro_parameter_{nodeId}_{portKey}";
	public static string ParameterBindingKey(Guid nodeId, string portKey) => $"macro_parameter_binding_{nodeId}_{portKey}";
	public static string ParameterOutputPortKey(string definitionId) => MacroParameterDefinitions.PortKey(definitionId);

	public static float ReadScalar(GraphValue value, EvaluationContext context)
	{
		return value switch
		{
			GraphValue.Scalar scalar => scalar.Value,
			GraphValue.Color color => (color.Value.R + color.Value.G + color.Value.B) / 3f,
			GraphValue.Raster raster => SampleField(raster.Value, context),
			GraphValue.Bitmap bitmap => SampleBitmap(bitmap.Value, context),
			_ => 0f
		};
	}

	public static Field ReadField(GraphValue value, int width, int height)
	{
		return value switch
		{
			GraphValue.Raster raster => raster.Value,
			GraphValue.Scalar scalar => FillField(width, height, scalar.Value),
			GraphValue.Color color => FillField(width, height, (color.Value.R + color.Value.G + color.Value.B) / 3f),
			GraphValue.Bitmap bitmap => ConvertBitmapToField(bitmap.Value),
			_ => new Field(width, height)
		};
	}

	public static RgbaBitmap ReadBitmap(GraphValue value, int width, int height)
	{
		return value switch
		{
			GraphValue.Bitmap bitmap => bitmap.Value,
			GraphValue.Color color => RgbaBitmap.Fill(width, height, color.Value),
			GraphValue.Scalar scalar => RgbaBitmap.Fill(width, height, new RgbaColor(scalar.Value, scalar.Value, scalar.Value, 1f)),
			GraphValue.Raster raster => ConvertFieldToBitmap(raster.Value, width, height),
			_ => RgbaBitmap.Fill(width, height, new RgbaColor(0f, 0f, 0f, 1f))
		};
	}

	public static RgbaColor ReadColor(GraphValue value, EvaluationContext context)
	{
		return value switch
		{
			GraphValue.Color color => color.Value,
			GraphValue.Scalar scalar => new RgbaColor(scalar.Value, scalar.Value, scalar.Value, 1f),
			GraphValue.Raster raster => CreateGrayscaleColor(SampleField(raster.Value, context)),
			GraphValue.Bitmap bitmap => SampleBitmapColor(bitmap.Value, context),
			_ => new RgbaColor(0f, 0f, 0f, 1f)
		};
	}

	public static PointSet ReadPoints(GraphValue value, int width, int height)
	{
		return value switch
		{
			GraphValue.Points points => points.Value,
			_ => PointSet.Empty(width, height)
		};
	}

	public static PathSet ReadPaths(GraphValue value, int width, int height)
	{
		return value switch
		{
			GraphValue.Paths paths => paths.Value,
			_ => PathSet.Empty(width, height)
		};
	}

	private static RgbaColor CreateGrayscaleColor(float value) =>
		new(value, value, value, 1f);

	private static Field FillField(int width, int height, float value)
	{
		var field = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			field[x, y] = value;
		return field;
	}

	private static Field ConvertBitmapToField(RgbaBitmap bitmap)
	{
		var field = new Field(bitmap.Width, bitmap.Height);
		for (var y = 0; y < bitmap.Height; y++)
		for (var x = 0; x < bitmap.Width; x++)
		{
			field[x, y] = (bitmap.R[x, y] + bitmap.G[x, y] + bitmap.B[x, y]) / 3f;
		}

		return field;
	}

	private static RgbaBitmap ConvertFieldToBitmap(Field field, int width, int height)
	{
		var r = new Field(width, height);
		var g = new Field(width, height);
		var b = new Field(width, height);
		var a = new Field(width, height);
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			var value = field[x, y];
			r[x, y] = value;
			g[x, y] = value;
			b[x, y] = value;
			a[x, y] = 1f;
		}

		return new RgbaBitmap(r, g, b, a);
	}

	private static float SampleField(Field field, EvaluationContext context)
	{
		var x = field.Width <= 1 ? 0 : Math.Clamp((int)MathF.Round(context.U * (field.Width - 1)), 0, field.Width - 1);
		var y = field.Height <= 1 ? 0 : Math.Clamp((int)MathF.Round(context.V * (field.Height - 1)), 0, field.Height - 1);
		return field[x, y];
	}

	private static float SampleBitmap(RgbaBitmap bitmap, EvaluationContext context)
	{
		var color = SampleBitmapColor(bitmap, context);
		return (color.R + color.G + color.B) / 3f;
	}

	private static RgbaColor SampleBitmapColor(RgbaBitmap bitmap, EvaluationContext context)
	{
		var x = bitmap.Width <= 1 ? 0 : Math.Clamp((int)MathF.Round(context.U * (bitmap.Width - 1)), 0, bitmap.Width - 1);
		var y = bitmap.Height <= 1 ? 0 : Math.Clamp((int)MathF.Round(context.V * (bitmap.Height - 1)), 0, bitmap.Height - 1);
		return new RgbaColor(bitmap.R[x, y], bitmap.G[x, y], bitmap.B[x, y], bitmap.A[x, y]);
	}
}
