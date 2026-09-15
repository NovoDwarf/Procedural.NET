using NovoDwarf.Primitives.Models;

namespace Procedural.NET.Core.Caching;

public class GraphCache
{
	private const int CacheCapacity = 512;

	public LruCache<FieldCacheKey, Field> Field { get; } = new(CacheCapacity);
	public LruCache<StructuredCacheKey, PointSet> PointSet { get; } = new(CacheCapacity);
	public LruCache<StructuredCacheKey, PathSet> PathSet { get; } = new(CacheCapacity);
	
	public bool TryGetField(Guid outputNodeId, string outputPortKey, int width, int height, out Field buffer)
		=> TryGet(Field, new FieldCacheKey(outputNodeId, outputPortKey, width, height), out buffer);

	public void SetField(Guid outputNodeId, string outputPortKey, int width, int height, Field buffer)
		=> Set(Field, new FieldCacheKey(outputNodeId, outputPortKey, width, height), buffer);

	public bool TryGetPointSet(Guid outputNodeId, string outputPortKey, int width, int height, out PointSet buffer)
		=> TryGet(PointSet, new StructuredCacheKey(outputNodeId, outputPortKey, width, height), out buffer);

	public void SetPointSet(Guid outputNodeId, string outputPortKey, int width, int height, PointSet buffer)
		=> Set(PointSet, new StructuredCacheKey(outputNodeId, outputPortKey, width, height), buffer);

	public bool TryGetPathSet(Guid outputNodeId, string outputPortKey, int width, int height, out PathSet buffer)
		=> TryGet(PathSet, new StructuredCacheKey(outputNodeId, outputPortKey, width, height), out buffer);

	public void SetPathSet(Guid outputNodeId, string outputPortKey, int width, int height, PathSet buffer)
		=> Set(PathSet, new StructuredCacheKey(outputNodeId, outputPortKey, width, height), buffer);
	
	public void Invalidate(HashSet<Guid> nodeIds)
	{
		Field.InvalidateWhere(k => nodeIds.Contains(k.OutputNodeId));
		PointSet.InvalidateWhere(k => nodeIds.Contains(k.OutputNodeId));
		PathSet.InvalidateWhere(k => nodeIds.Contains(k.OutputNodeId));
	}

	public void Clear()
	{
		Field.Clear();
		PointSet.Clear();
		PathSet.Clear();
	}
	
	private static bool TryGet<TKey, TValue>(LruCache<TKey, TValue> cache, TKey key, out TValue value) 
		where TValue : class 
		where TKey : notnull
	{
		return cache.TryGet(key, out value);
	}

	private static void Set<TKey, TValue>(LruCache<TKey, TValue> cache, TKey key, TValue value) 
		where TValue : class 
		where TKey : notnull
	{
		cache.Set(key, value);
	}
}
