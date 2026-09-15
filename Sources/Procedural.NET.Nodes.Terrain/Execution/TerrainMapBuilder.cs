using NovoDwarf.Primitives.Models;


namespace Procedural.NET.Nodes.Terrain.Execution;

/// <summary>
/// Generates terrain maps (normal, splat) from elevation <see cref="Field"/> data.
/// All methods are pure and allocation-light — one <see cref="RgbaBitmap"/> per call.
/// </summary>
public static class TerrainMapBuilder
{
    /// <summary>
    /// Height-to-world-width ratio shared with <c>TerrainMeshBuilder</c> and the terrain shader.
    /// Changing this value must be reflected in the shader uniform <c>u_height_scale</c>.
    /// </summary>
    public const float TerrainHeightScale = 0.08f;
    private const float TopographicMinorContourStep = 0.025f;
    private const int TopographicMajorContourEvery = 5;

    // ── Ambient occlusion ────────────────────────────────────────────────────

    /// <summary>
    /// Builds a horizon-based ambient occlusion map from the elevation field.
    /// For each pixel, casts <paramref name="rayCount"/> rays around the full circle
    /// and records the maximum upward horizon angle per direction.
    /// The result is a single-channel <see cref="Field"/> in 0..1 range (1 = unoccluded).
    /// </summary>
    /// <param name="heights">Source elevation field.</param>
    /// <param name="rayCount">Number of azimuth rays (8 gives good quality with low cost).</param>
    /// <param name="raySteps">March steps per ray (stops early at field boundary).</param>
    /// <param name="heightScale">Height-to-world-width ratio (matches terrain shader).</param>
    public static Field BuildAmbientOcclusionMap(
        Field heights,
        int   rayCount   = 8,
        int   raySteps   = 8,
        float heightScale = TerrainHeightScale)
    {
        var w  = heights.Width;
        var h  = heights.Height;
        // Convert field ΔElevation to world-normalized vertical units so the
        // aspect ratio matches the 3-D terrain (worldSize cancels out when
        // computing tan(horizon) = dH_world / dist_world).
        var fieldScale = heightScale * Math.Max(w - 1, h - 1);

        var aoField = new Field(w, h);

        for (var z = 0; z < h; z++)
        for (var x = 0; x < w; x++)
        {
            var h0           = heights.GetUnchecked(x, z);
            var totalOcclude = 0f;

            for (var r = 0; r < rayCount; r++)
            {
                var angle = r * MathF.Tau / rayCount;
                var ddx   = MathF.Cos(angle);
                var ddz   = MathF.Sin(angle);
                var maxTan = float.MinValue;

                for (var s = 1; s <= raySteps; s++)
                {
                    var sx = (int)MathF.Round(x + ddx * s);
                    var sz = (int)MathF.Round(z + ddz * s);
                    if ((uint)sx >= (uint)w || (uint)sz >= (uint)h) break;

                    var actualDist = MathF.Sqrt((float)((sx - x) * (sx - x) + (sz - z) * (sz - z)));
                    var horizonTan = (heights.GetUnchecked(sx, sz) - h0) * fieldScale / actualDist;
                    if (horizonTan > maxTan) maxTan = horizonTan;
                }

                if (maxTan > 0f)
                {
                    // sin = tan / sqrt(1 + tan²)
                    totalOcclude += maxTan / MathF.Sqrt(1f + maxTan * maxTan);
                }
            }

            aoField.SetUnchecked(x, z, 1f - totalOcclude / rayCount);
        }

        return aoField;
    }

    // ── Cavity map ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a cavity map using the discrete Laplacian of the elevation field.
    /// Values above 0.5 = convex (ridges/peaks), below 0.5 = concave (valleys/crevices).
    /// </summary>
    public static Field BuildCavityMap(Field heights, float strength = 3f)
    {
        var w  = heights.Width;
        var h  = heights.Height;
        var w1 = w - 1;
        var h1 = h - 1;
        var cavity = new Field(w, h);

        for (var z = 0; z < h; z++)
        for (var x = 0; x < w; x++)
        {
            var center = heights.GetUnchecked(x, z) * 4f;
            var left   = heights.GetUnchecked(Math.Max(x - 1, 0),  z);
            var right  = heights.GetUnchecked(Math.Min(x + 1, w1), z);
            var down   = heights.GetUnchecked(x, Math.Max(z - 1, 0));
            var up     = heights.GetUnchecked(x, Math.Min(z + 1, h1));

            // Laplacian: positive = convex, negative = concave.
            var laplacian = center - left - right - down - up;
            cavity.SetUnchecked(x, z, Math.Clamp(0.5f + laplacian * strength, 0f, 1f));
        }

        return cavity;
    }

    // ── Flow / wetness map ───────────────────────────────────────────────────

    /// <summary>
    /// Approximates flow accumulation using negative curvature (concave areas collect water).
    /// A fast O(W·H) estimate — for high-accuracy flow use a proper D8/D∞ router.
    /// Result is 0..1 where 1 = strong flow/wetness.
    /// </summary>
    public static Field BuildFlowMap(Field heights, float strength = 4f)
    {
        var w  = heights.Width;
        var h  = heights.Height;
        var w1 = w - 1;
        var h1 = h - 1;
        var flow = new Field(w, h);

        for (var z = 0; z < h; z++)
        for (var x = 0; x < w; x++)
        {
            var center = heights.GetUnchecked(x, z) * 4f;
            var left   = heights.GetUnchecked(Math.Max(x - 1, 0),  z);
            var right  = heights.GetUnchecked(Math.Min(x + 1, w1), z);
            var down   = heights.GetUnchecked(x, Math.Max(z - 1, 0));
            var up     = heights.GetUnchecked(x, Math.Min(z + 1, h1));

            // Negative Laplacian = concave → accumulates flow.
            var concavity = -(center - left - right - down - up);
            flow.SetUnchecked(x, z, Math.Clamp(concavity * strength, 0f, 1f));
        }

        return flow;
    }

    /// <summary>
    /// Builds a topographic color map with hillshade and contour lines.
    /// Intended for atlas/map preview rather than terrain texturing.
    /// </summary>
    public static RgbaBitmap BuildTopographicMap(Field heights)
    {
        var w = heights.Width;
        var h = heights.Height;
        var w1 = w - 1;
        var h1 = h - 1;

        var r = new Field(w, h);
        var g = new Field(w, h);
        var b = new Field(w, h);
        var a = new Field(w, h);

        for (var z = 0; z < h; z++)
        for (var x = 0; x < w; x++)
        {
            var elev = heights.GetUnchecked(x, z);
            var baseColor = TopographicPalette(elev);
            var hillshade = ComputeHillshade(heights, x, z, w1, h1);
            var minorContour = ComputeContourStrength(elev, TopographicMinorContourStep, 0.17f);
            var majorContour = ComputeContourStrength(elev, TopographicMinorContourStep * TopographicMajorContourEvery, 0.22f);

            var terrainColor = MultiplyColor(
                LerpColor(baseColor, new RgbaColor(0.96f, 0.95f, 0.90f, 1f), 0.14f),
                0.62f + hillshade * 0.38f);
            var contourColor = new RgbaColor(0.25f, 0.21f, 0.16f, 1f);
            var color = LerpColor(
                LerpColor(terrainColor, contourColor, minorContour * 0.38f),
                contourColor,
                majorContour * 0.72f);

            r.SetUnchecked(x, z, color.R);
            g.SetUnchecked(x, z, color.G);
            b.SetUnchecked(x, z, color.B);
            a.SetUnchecked(x, z, 1f);
        }

        return new RgbaBitmap(r, g, b, a);
    }

    // ── Normal map ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a world-space normal map from the elevation field.
    /// Normals are packed to 0..1: <c>rgb = (n + 1) * 0.5</c>.
    /// <paramref name="heightScale"/> is the height-to-world-width ratio
    /// (default matches <c>TerrainMeshBuilder.TerrainHeightRatio</c>).
    /// </summary>
    public static RgbaBitmap BuildNormalMap(Field heights, float heightScale = TerrainHeightScale)
    {
        var w = heights.Width;
        var h = heights.Height;
        var w1 = w - 1;
        var h1 = h - 1;

        var r = new Field(w, h);
        var g = new Field(w, h);
        var b = new Field(w, h);
        var a = new Field(w, h);

        for (var z = 0; z < h; z++)
        for (var x = 0; x < w; x++)
        {
            // Central differences; worldSize cancels when normalizing.
            var dx = (heights[Math.Min(x + 1, w1), z] - heights[Math.Max(x - 1, 0), z])
                     * w1 * heightScale * 0.5f;
            var dz = (heights[x, Math.Min(z + 1, h1)] - heights[x, Math.Max(z - 1, 0)])
                     * h1 * heightScale * 0.5f;

            // Surface normal in world space: -gradient, upward component = 1.
            var len = MathF.Sqrt(dx * dx + 1f + dz * dz);
            var nx = -dx / len;
            var ny = 1f  / len;
            var nz = -dz / len;

            // Pack to 0..1.
            r.SetUnchecked(x, z, nx * 0.5f + 0.5f);
            g.SetUnchecked(x, z, ny * 0.5f + 0.5f);
            b.SetUnchecked(x, z, nz * 0.5f + 0.5f);
            a.SetUnchecked(x, z, 1f);
        }

        return new RgbaBitmap(r, g, b, a);
    }

    // ── Splat map ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a procedural 4-channel splat map from elevation and slope.
    /// Layer convention (matches <c>terrain_splat.gdshader</c>):
    /// <list type="bullet">
    ///   <item>R — grass (low altitude, gentle slope)</item>
    ///   <item>G — ground/dirt (mid altitude)</item>
    ///   <item>B — rock (steep slope, high altitude)</item>
    ///   <item>A — snow/peaks (very high altitude)</item>
    /// </list>
    /// Channels are normalised so they always sum to 1.
    /// </summary>
    public static RgbaBitmap BuildProceduralSplatMap(Field heights)
    {
        var w = heights.Width;
        var h = heights.Height;
        var w1 = w - 1;
        var h1 = h - 1;

        var rF = new Field(w, h);
        var gF = new Field(w, h);
        var bF = new Field(w, h);
        var aF = new Field(w, h);

        for (var z = 0; z < h; z++)
        for (var x = 0; x < w; x++)
        {
            var elev  = heights.GetUnchecked(x, z);
            var slope = ComputeSlope(heights, x, z, w1, h1);

            var wGrass  = (1f - slope) * (1f - SmoothStep(0.30f, 0.55f, elev));
            var wDirt   = SmoothStep(0.20f, 0.45f, elev) * (1f - SmoothStep(0.55f, 0.75f, elev))
                          * (1f - slope * 0.6f);
            var wRock   = slope * 0.7f + SmoothStep(0.55f, 0.80f, elev) * (1f - SmoothStep(0.88f, 0.96f, elev));
            var wSnow   = SmoothStep(0.78f, 0.95f, elev);

            var sum = wGrass + wDirt + wRock + wSnow;
            if (sum < 1e-6f) { sum = 1f; wGrass = 1f; }

            rF.SetUnchecked(x, z, wGrass / sum);
            gF.SetUnchecked(x, z, wDirt  / sum);
            bF.SetUnchecked(x, z, wRock  / sum);
            aF.SetUnchecked(x, z, wSnow  / sum);
        }

        return new RgbaBitmap(rF, gF, bF, aF);
    }

    /// <summary>
    /// Assembles a splat map from up to 4 explicit density fields.
    /// Missing channels default to 0 (except R which claims any remainder).
    /// Channels are normalised to sum to 1.
    /// </summary>
    public static RgbaBitmap AssembleSplatMap(Field r, Field? g = null, Field? b = null, Field? a = null)
    {
        var w = r.Width;
        var h = r.Height;
        var rF = new Field(w, h);
        var gF = new Field(w, h);
        var bF = new Field(w, h);
        var aF = new Field(w, h);

        for (var z = 0; z < h; z++)
        for (var x = 0; x < w; x++)
        {
            var wr = Math.Max(0f, r.GetUnchecked(x, z));
            var wg = g is not null ? Math.Max(0f, g.GetUnchecked(x, z)) : 0f;
            var wb = b is not null ? Math.Max(0f, b.GetUnchecked(x, z)) : 0f;
            var wa = a is not null ? Math.Max(0f, a.GetUnchecked(x, z)) : 0f;

            var sum = wr + wg + wb + wa;
            if (sum < 1e-6f) { sum = 1f; wr = 1f; }

            rF.SetUnchecked(x, z, wr / sum);
            gF.SetUnchecked(x, z, wg / sum);
            bF.SetUnchecked(x, z, wb / sum);
            aF.SetUnchecked(x, z, wa / sum);
        }

        return new RgbaBitmap(rF, gF, bF, aF);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static float ComputeSlope(Field h, int x, int z, int w1, int h1)
    {
        var dx = h[Math.Min(x + 1, w1), z] - h[Math.Max(x - 1, 0), z];
        var dz = h[x, Math.Min(z + 1, h1)] - h[x, Math.Max(z - 1, 0)];
        return Math.Clamp(MathF.Sqrt(dx * dx + dz * dz) * 5f, 0f, 1f);
    }

    private static float ComputeHillshade(Field heights, int x, int z, int w1, int h1)
    {
        var dx = (heights[Math.Min(x + 1, w1), z] - heights[Math.Max(x - 1, 0), z]) * w1 * TerrainHeightScale * 0.5f;
        var dz = (heights[x, Math.Min(z + 1, h1)] - heights[x, Math.Max(z - 1, 0)]) * h1 * TerrainHeightScale * 0.5f;
        var len = MathF.Sqrt(dx * dx + 1f + dz * dz);
        var nx = -dx / len;
        var ny = 1f / len;
        var nz = -dz / len;

        const float lx = 0.500f;
        const float ly = 0.707f;
        const float lz = -0.500f;
        return Math.Clamp(nx * lx + ny * ly + nz * lz, 0f, 1f);
    }

    private static float ComputeContourStrength(float elevation, float step, float width)
    {
        var normalized = elevation / step;
        var distanceToLine = MathF.Abs(normalized - MathF.Round(normalized));
        return Math.Clamp(1f - distanceToLine / width, 0f, 1f);
    }

    private static float SmoothStep(float edge0, float edge1, float t)
    {
        t = Math.Clamp((t - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static RgbaColor TopographicPalette(float value) => value switch
    {
        < 0.24f => LerpColor(new RgbaColor(0.70f, 0.81f, 0.59f, 1f), new RgbaColor(0.58f, 0.73f, 0.48f, 1f), value / 0.24f),
        < 0.52f => LerpColor(new RgbaColor(0.58f, 0.73f, 0.48f, 1f), new RgbaColor(0.78f, 0.74f, 0.57f, 1f), (value - 0.24f) / 0.28f),
        < 0.78f => LerpColor(new RgbaColor(0.78f, 0.74f, 0.57f, 1f), new RgbaColor(0.63f, 0.57f, 0.46f, 1f), (value - 0.52f) / 0.26f),
        _ => LerpColor(new RgbaColor(0.63f, 0.57f, 0.46f, 1f), new RgbaColor(0.91f, 0.89f, 0.84f, 1f), (value - 0.78f) / 0.22f)
    };

    private static RgbaColor LerpColor(RgbaColor a, RgbaColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new RgbaColor(
            a.R + (b.R - a.R) * t,
            a.G + (b.G - a.G) * t,
            a.B + (b.B - a.B) * t,
            a.A + (b.A - a.A) * t);
    }

    private static RgbaColor MultiplyColor(RgbaColor color, float factor)
    {
        return new RgbaColor(color.R * factor, color.G * factor, color.B * factor, color.A).Clamp();
    }
}
