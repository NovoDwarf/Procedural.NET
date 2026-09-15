namespace Procedural.NET.Core.Caching;

public readonly record struct StructuredCacheKey(Guid OutputNodeId, string OutputPortKey, int Width, int Height);
