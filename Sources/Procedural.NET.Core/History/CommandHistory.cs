namespace Procedural.NET.Core.History;

public sealed class CommandHistory
{
	private readonly List<CommandRecord> _undo = [];
	private readonly List<CommandRecord> _redo = [];

	private List<CommandRecord>? _batch;
	private string? _batchName;

	public int MaxCapacity { get; set; } = 100;

	public IReadOnlyList<CommandRecord> UndoItems => _undo;
	public IReadOnlyList<CommandRecord> RedoItems => _redo;

	public bool CanUndo => _undo.Count > 0;
	public bool CanRedo => _redo.Count > 0;
	public bool InBatch => _batch is not null;

	public event Action? Changed;
	
	public void Execute(string name, Action redo, Action undo)
	{
		redo();
		var record = new CommandRecord(name, redo, undo);

		if (_batch is not null)
		{
			_batch.Add(record);
			return;
		}

		Push(record);
	}
	
	public void Undo()
	{
		if (_undo.Count == 0)
			return;

		var cmd = _undo[^1];
		_undo.RemoveAt(_undo.Count - 1);
		cmd.Undo();
		_redo.Add(cmd);
		Changed?.Invoke();
	}

	public void Redo()
	{
		if (_redo.Count == 0)
			return;

		var cmd = _redo[^1];
		_redo.RemoveAt(_redo.Count - 1);
		cmd.Redo();
		_undo.Add(cmd);
		Changed?.Invoke();
	}

	public void UndoMany(int count)
	{
		var n = Math.Min(count, _undo.Count);
		for (var i = 0; i < n; i++)
		{
			var cmd = _undo[^1];
			_undo.RemoveAt(_undo.Count - 1);
			cmd.Undo();
			_redo.Add(cmd);
		}

		if (n > 0)
			Changed?.Invoke();
	}

	public void RedoMany(int count)
	{
		var n = Math.Min(count, _redo.Count);
		for (var i = 0; i < n; i++)
		{
			var cmd = _redo[^1];
			_redo.RemoveAt(_redo.Count - 1);
			cmd.Redo();
			_undo.Add(cmd);
		}

		if (n > 0)
			Changed?.Invoke();
	}
	
	public void BeginBatch(string name)
	{
		_batchName = name;
		_batch     = [];
	}

	public void CommitBatch()
	{
		var items = _batch;
		var name  = _batchName ?? string.Empty;
		_batch    = null;
		_batchName = null;

		if (items is null || items.Count == 0)
			return;

		var snapshot = items.ToArray();
		Push(new CommandRecord(
			name,
			() => { foreach (var r in snapshot) r.Redo(); },
			() => { for (var i = snapshot.Length - 1; i >= 0; i--) snapshot[i].Undo(); }
		));
	}

	public void RollbackBatch()
	{
		var items = _batch;
		_batch    = null;
		_batchName = null;

		if (items is null)
			return;

		for (var i = items.Count - 1; i >= 0; i--)
			items[i].Undo();
	}
	
	public void Clear()
	{
		_undo.Clear();
		_redo.Clear();
		_batch = null;
		Changed?.Invoke();
	}
	
	private void Push(CommandRecord record)
	{
		if (_undo.Count >= MaxCapacity)
			_undo.RemoveAt(0);

		_undo.Add(record);
		_redo.Clear();
		Changed?.Invoke();
	}
}
