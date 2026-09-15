namespace Procedural.NET.Core.Execution;

public abstract record EvaluationDomain
{
	public sealed record Sample(float U, float V) : EvaluationDomain;
	public sealed record Raster(int Width, int Height) : EvaluationDomain;
}
