namespace Procedural.NET.Core.Caching;

public readonly record struct FieldCacheKey(Guid OutputNodeId, string OutputPortKey, int Width, int Height);
