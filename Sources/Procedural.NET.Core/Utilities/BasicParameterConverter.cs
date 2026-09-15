namespace Procedural.NET.Core.Utilities;

public static class BasicParameterConverter
{
	public static T Convert<T>(float value, string parameterKey) => (T)Convert(value, typeof(T), parameterKey);

	public static object Convert(float value, Type targetType, string parameterKey)
	{
		if (targetType == typeof(float))
			return value;

		if (targetType == typeof(double))
			return (double)value;

		if (targetType == typeof(int))
			return (int)MathF.Round(value);

		if (targetType == typeof(bool))
			return value >= 0.5f;

		if (targetType.IsEnum)
			return Enum.ToObject(targetType, (int)MathF.Round(value));

		throw new NotSupportedException(
			$"Parameter [{parameterKey}] cannot be converted to [{targetType.Name}]. " +
			$"Supported target types: float, double, int, bool, enum.");
	}
}
