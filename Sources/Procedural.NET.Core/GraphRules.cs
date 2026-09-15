using Procedural.NET.Core.Enums;

namespace Procedural.NET.Core;

public static class GraphRules
{
	public const string ParameterPortPrefix = "param:";

	public static string ParameterPortKey(string parameterKey) => $"{ParameterPortPrefix}{parameterKey}";

	public static bool IsParameterPortKey(string portKey)
		=> portKey.StartsWith(ParameterPortPrefix, StringComparison.Ordinal);

	public static string ParameterKeyFromPortKey(string portKey)
		=> IsParameterPortKey(portKey) ? portKey[ParameterPortPrefix.Length..] : portKey;

	public static bool ArePortsCompatible(GraphNodePort output, GraphNodePort input)
		=> output.Shape == ValueShape.Color && input.Shape == ValueShape.Bitmap ||
		   AreShapesCompatible(output.Shape, input.Shape) &&
		   AreSemanticsCompatible(output.Semantics, input.Semantics);

	public static bool AreShapesCompatible(ValueShape output, ValueShape input)
	{
		if (input == ValueShape.Any || output == ValueShape.Any)
			return true;

		if (output == input)
			return true;

		if (IsScalarGroup(output) && IsScalarGroup(input))
			return true;

		if (IsVector2Group(output) && IsVector2Group(input))
			return true;

		if (IsVector3Group(output) && IsVector3Group(input))
			return true;

		if (IsScalarGroup(output) && input == ValueShape.Field)
			return true;

		if (output == ValueShape.Color && input == ValueShape.Bitmap)
			return true;

		return false;
	}

	public static bool AreSemanticsCompatible(PortSemantics output, PortSemantics input)
		=> input == PortSemantics.Generic ||
		   output == PortSemantics.Generic ||
		   output == input;

	public static bool IsScalarGroup(ValueShape shape) => shape is ValueShape.Float or ValueShape.Integer or ValueShape.Boolean;

	public static bool IsVector2Group(ValueShape shape) => shape is ValueShape.Float2 or ValueShape.Int2;

	public static bool IsVector3Group(ValueShape shape) => shape is ValueShape.Float3 or ValueShape.Int3;

	public static bool IsFieldGroup(ValueShape shape) => shape is ValueShape.Field or ValueShape.Volume;
	public static bool IsSpatial(ValueShape shape) => shape is ValueShape.Points or ValueShape.Paths;

	public static bool IsBitmap(ValueShape shape) => shape == ValueShape.Bitmap;

	public static bool IsColor(ValueShape shape) => shape == ValueShape.Color;
}
