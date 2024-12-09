using DocDB.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace DocDB
{
    internal static class TocWriter
    {
        // Tables
        // Views
        // Programmability
        //  Stored Procedures
        //  Functions
        //      Table-valued Functions
        //      Scalar-valued Functions
        //      Aggregate Functions
        //  Database Triggers
        //  Assemblies
        //  Types
        //      User-Defined Data Types
        //      User-Defined Table Types
        //      User-Defined Types
        //      XML Schema Collections
        //  Sequences
        // Storage
        //  Partition Schemas
        //  Partition Functions
        // Security
        //  Users
        //  Roles
        //      Database Roles
        //      Application Roles
        //  


        public static void WriteToc(string fileName, ConcurrentDictionary<string, List<DdbObject>> objects)
        {
            using var stream = new StreamWriter(fileName, append: false);
            var emitter = new Emitter(stream);
            emitter.Emit(new StreamStart());
            emitter.Emit(new Comment($"YamlMime:DocDB_TableOfContents", false));
            emitter.Emit(new DocumentStart());

            emitter.Emit(MappingStart());
            emitter.Emit(new Scalar("items"));
            emitter.Emit(SequenceStart());

            AddNamedItemSequence(emitter, "Tables", GetObjects<DdbTable>(objects));
            AddNamedItemSequence(emitter, "Views", GetObjects<DdbView>(objects));

            NamedComplexSequenceEntry(emitter, "Programmability", emitter =>
            {
                emitter.Emit(new Scalar("items"));
                emitter.Emit(SequenceStart());

                AddNamedItemSequence(emitter, "Stored Procedures", GetObjects<DdbStoredProcedure>(objects));

                NamedComplexSequenceEntry(emitter, "Functions", emitter =>
                {
                    emitter.Emit(new Scalar("items"));
                    emitter.Emit(SequenceStart());

                    var functions = GetObjects<DdbUserDefinedFunction>(objects);

                    // SSMS also treats "inline" as "table"
                    var tableValued = functions.Where(u =>
                            u.FunctionType == DdbUserDefinedFunctionType.Table ||
                            u.FunctionType == DdbUserDefinedFunctionType.Inline)
                            .ToList();
                    var scalarValued = functions.Where(u => u.FunctionType == DdbUserDefinedFunctionType.Scalar).ToList();

                    AddNamedItemSequence(emitter, "Table-valued Functions", tableValued);
                    AddNamedItemSequence(emitter, "Scalar-valued Functions", scalarValued);
                    AddNamedItemSequence(emitter, "Aggregate Functions", GetObjects<DbdUserDefinedAggregate>(objects));

                    emitter.Emit(SequenceEnd());
                });


                emitter.Emit(SequenceEnd());
            });

            emitter.Emit(SequenceEnd());
            emitter.Emit(MappingEnd());

            emitter.Emit(new DocumentEnd(false));
            emitter.Emit(new StreamEnd());
        }

        static List<T> GetObjects<T>(IDictionary<string, List<DdbObject>> objects) where T : DdbObject
        {
            if (objects.TryGetValue(typeof(T).Name, out var types))
            {
                return types.OfType<T>().ToList();
            }

            return [];
        }

        static void NamedComplexSequenceEntry(IEmitter emitter, string name, Action<IEmitter> content)
        {
            emitter.Emit(MappingStart());
            emitter.Emit(new Scalar("name"));
            emitter.Emit(new Scalar(name));

            content(emitter);

            emitter.Emit(MappingEnd());
        }

        static void AddNamedItemSequence<T>(IEmitter emitter, string name, List<T>? items) where T : DdbObject
        {
            NamedComplexSequenceEntry(emitter, name, e => AddItemSequence(items, e));
        }

        static void AddItemSequence<T>(List<T>? items, IEmitter emitter) where T : DdbObject
        {
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
                        emitter.Emit(new Scalar("name"));
                        emitter.Emit(new Scalar(named.Name));
                    }
                    emitter.Emit(MappingEnd());
                }
            }

            emitter.Emit(SequenceEnd());
        }

        static SequenceStart SequenceStart() => new(null, null, true, SequenceStyle.Block);
        static SequenceEnd SequenceEnd() => new();
        static MappingStart MappingStart() => new(null, null, true, MappingStyle.Block);
        static MappingEnd MappingEnd() => new();
    }
}