namespace Procedural.NET.Core.History;

public sealed record CommandRecord(string Name, Action Redo, Action Undo);
