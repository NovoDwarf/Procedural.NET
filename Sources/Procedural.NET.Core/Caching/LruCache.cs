namespace Procedural.NET.Core.Caching;

public sealed class LruCache<TKey, TValue> where TKey : notnull where TValue : class
{
	private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _index = [];
	private readonly LinkedList<(TKey Key, TValue Value)> _list = new();
	private readonly int _capacity;

	public LruCache(int capacity)
	{
		_capacity = capacity;
	}

	public bool TryGet(TKey key, out TValue value)
	{
		if (!_index.TryGetValue(key, out var node)) { value = null!; return false; }
		_list.Remove(node);
		_list.AddFirst(node);
		value = node.Value.Value;
		return true;
	}

	public void Set(TKey key, TValue value)
	{
		if (_index.TryGetValue(key, out var existing))
		{
			_list.Remove(existing);
			_index.Remove(key);
		}
		else if (_list.Count >= _capacity)
		{
			var lru = _list.Last!;
			_index.Remove(lru.Value.Key);
			_list.RemoveLast();
		}

		var node = _list.AddFirst((key, value));
		_index[key] = node;
	}

	public void InvalidateWhere(Func<TKey, bool> predicate)
	{
		var toRemove = _index.Keys.Where(predicate).ToList();
		foreach (var key in toRemove)
		{
			_list.Remove(_index[key]);
			_index.Remove(key);
		}
	}

	public void Clear()
	{
		_index.Clear();
		_list.Clear();
	}
}