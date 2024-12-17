using DocDB;
using System;
using System.Collections.Generic;
using System.Linq;

internal class UidBuilder
{
    private class Scope : IDisposable
    {
        private readonly UidBuilder _builder;

        public Scope(UidBuilder builder, string name, string title)
        {
            _builder = builder;
            Id = name;
            Name = title;
        }
        public string Id { get; }
        public string Name { get; }
        public Dictionary<string, (string Name, string? Description)> Entries { get; set; } = new(StringComparer.Ordinal);
        public void Dispose()
        {
            try
            {
                _builder._writer.WriteDoc(_builder.Value, Name, Entries);
            }
            finally
            {
                _builder._path.Pop();
            }
        }
    }

    private readonly TocSectionWriter _writer;
    private readonly Stack<Scope> _path = new();

    public UidBuilder(string id, string name, TocSectionWriter writer)
    {
        _writer = writer;
        _path.Push(new(this, id.AsIdentifier(), name));
    }

    public IDisposable GetScope(string id, string name)
    {
        var scope = new Scope(this, id.AsIdentifier(), name);
        _path.Push(scope);
        return scope;
    }

    public void AddEntry(string uid, string name, string? description) => _path.Peek().Entries[uid] = (name, description);
    public string Value => string.Join(".", _path.Select(p => p.Id).Reverse());
}