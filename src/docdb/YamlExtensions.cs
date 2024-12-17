using DocDB.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

internal static class YamlExtensions
{
    public static void EmitNamedScalar(this IEmitter emitter, string name, string? value)
    {
        emitter.Emit(new Scalar(name));
        if (value != null)
        {
            emitter.Emit(new Scalar(value));
        }
        else
        {
            emitter.Emit(new Scalar(null, null, "", ScalarStyle.ForcePlain, true, true));
        }
    }

    public static void EmitNamedItemSequence<T>(this IEmitter emitter, string name, UidBuilder uidBuilder, IEnumerable<T>? items) where T : DdbObject
    {
        emitter.EmitNamed(name, emitter =>
        {
            string uid = AddItemSequence(uidBuilder, name, items, emitter);
            uidBuilder.AddEntry(uid, name, null);
        });
    }

    public static void EmitNamed(this IEmitter emitter, string name, Action<IEmitter> content)
    {
        emitter.Emit(MappingStart());
        emitter.EmitNamedScalar("name", name);

        content(emitter);

        emitter.Emit(MappingEnd());
    }

    private static string AddItemSequence<T>(UidBuilder uidBuilder, string name, IEnumerable<T>? items, IEmitter emitter) where T : DdbObject
    {
        using var scope = uidBuilder.GetScope(DdbObject.GetTypeTag<T>(), name);
        string uid = uidBuilder.Value;
        emitter.EmitNamedScalar("uid", uid);
        emitter.Emit(new Scalar("items"));
        emitter.Emit(SequenceStart());

        if (items != null)
        {
            foreach (var item in items.OrderBy(t => t.Id))
            {
                emitter.Emit(MappingStart());
                emitter.Emit(new Scalar("uid"));
                emitter.Emit(new Scalar(item.Id));

                if (item is NamedDdbObject named)
                {
                    emitter.EmitNamedScalar("name", named.Name);

                    uidBuilder.AddEntry(item.Id, named.Name, named.Description);
                }
                else
                {
                    uidBuilder.AddEntry(item.Id, item.Type, item.Description);
                }

                emitter.EmitNamedScalar("type", item.Type);
                emitter.Emit(MappingEnd());
            }
        }

        emitter.Emit(SequenceEnd());
        return uid;
    }

    public static SequenceStart SequenceStart() => new(null, null, true, SequenceStyle.Block);
    public static SequenceEnd SequenceEnd() => new();
    public static MappingStart MappingStart() => new(null, null, true, MappingStyle.Block);
    public static MappingEnd MappingEnd() => new();
}
