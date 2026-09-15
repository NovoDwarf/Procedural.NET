namespace Procedural.NET.Nodes.Terrain.Natures;

public sealed class HydraulicErosionSettings
{
	public int Iterations { get; init; } = 50000;
	public int MaxLifetime { get; init; } = 40;

	public float Inertia { get; init; } = 0.05f;
	public float SedimentCapacityFactor { get; init; } = 4.0f;
	public float MinSedimentCapacity { get; init; } = 0.01f;

	public float ErodeSpeed { get; init; } = 0.3f;
	public float DepositSpeed { get; init; } = 0.3f;
	public float EvaporateSpeed { get; init; } = 0.01f;
	public float Gravity { get; init; } = 4.0f;

	public float InitialWater { get; init; } = 1.0f;
	public float InitialSpeed { get; init; } = 1.0f;

	public int ErosionRadius { get; init; } = 3;
}