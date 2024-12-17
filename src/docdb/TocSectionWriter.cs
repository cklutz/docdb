using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

using static YamlExtensions;

internal class TocSectionWriter
{
    private readonly string _databaseId;
    private readonly string _directoryName;

    public TocSectionWriter(string databaseId, string directoryName)
    {
        _databaseId = databaseId;
        _directoryName = directoryName;
    }

    public void WriteDoc(string id, string name, IDictionary<string, (string Name, string? Description)> entries)
    {
        string fileName = Path.Combine(_directoryName, id + ".yml");
        Console.WriteLine(">>>> " + fileName);
        using var stream = new StreamWriter(fileName, append: false);
        stream.WriteLine("### YamlMime:DocDB");
        var emitter = new Emitter(stream);
        emitter.Emit(new StreamStart());
        emitter.Emit(new DocumentStart());

        emitter.Emit(MappingStart());
        emitter.EmitNamedScalar("type", "TocSection");
        emitter.EmitNamedScalar("id", id);
        emitter.EmitNamedScalar("description", "");
        emitter.EmitNamedScalar("name", name);
        emitter.EmitNamedScalar("databaseId", _databaseId);
        emitter.EmitNamedScalar("script", "");
        emitter.Emit(new Scalar("items"));
        emitter.Emit(SequenceStart());

        foreach (var entry in entries)
        {
            emitter.Emit(MappingStart());
            emitter.EmitNamedScalar("id", entry.Key);
            emitter.EmitNamedScalar("name", entry.Value.Name);
            emitter.EmitNamedScalar("description", entry.Value.Description ?? "");
            emitter.Emit(MappingEnd());
        }

        emitter.Emit(SequenceEnd());
        emitter.Emit(MappingEnd());

        emitter.Emit(new DocumentEnd(false));
        emitter.Emit(new StreamEnd());
    }
}
