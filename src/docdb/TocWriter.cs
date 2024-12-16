using DocDB.Contracts;
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
        // "Database"
        //      Tables
        //      Views
        //      Programmability
        //          Stored Procedures
        //          Functions
        //              Table-valued Functions
        //              Scalar-valued Functions
        //              Aggregate Functions
        //          Database Triggers
        //          Assemblies
        //          Types
        //              User-Defined Data Types
        //              User-Defined Table Types
        //              User-Defined Types
        //              XML Schema Collections
        //          Sequences
        //      Storage
        //          Partition Schemas
        //          Partition Functions
        //      Security
        //          Users
        //          Roles
        //              Database Roles
        //              Application Roles
        //          Schemas
        //  


        public static void WriteToc(string fileName, List<DdbObject> objects)
        {
            var databaseObj = objects.OfType<DdbDatabase>().FirstOrDefault();
            if (databaseObj == null)
            {
                throw new InvalidOperationException("Objects contain no database object.");
            }

            using var stream = new StreamWriter(fileName, append: false);
            stream.WriteLine("### YamlMime:TableOfContent");
            var emitter = new Emitter(stream);
            emitter.Emit(new StreamStart());
            emitter.Emit(new DocumentStart());

            emitter.Emit(MappingStart());
            emitter.Emit(new Scalar("items"));
            emitter.Emit(SequenceStart());

            NamedComplexSequenceEntry(emitter, databaseObj.Name, emitter =>
            {
                emitter.Emit(new Scalar("uid"));
                emitter.Emit(new Scalar(databaseObj.Id));
                emitter.Emit(new Scalar("type"));
                emitter.Emit(new Scalar(databaseObj.Type));
                emitter.Emit(new Scalar("items"));
                emitter.Emit(SequenceStart());

                AddNamedItemSequence(emitter, "Tables", objects.OfType<DdbTable>());
                AddNamedItemSequence(emitter, "Views", objects.OfType<DdbView>());

                NamedComplexSequenceEntry(emitter, "Programmability", emitter =>
                {
                    emitter.Emit(new Scalar("items"));
                    emitter.Emit(SequenceStart());

                    AddNamedItemSequence(emitter, "Stored Procedures", objects.OfType<DdbStoredProcedure>());

                    NamedComplexSequenceEntry(emitter, "Functions", emitter =>
                    {
                        emitter.Emit(new Scalar("items"));
                        emitter.Emit(SequenceStart());

                        var functions = objects.OfType<DdbUserDefinedFunction>();

                        // SSMS also treats "inline" as "table"
                        var tableValued = functions.Where(u =>
                                u.FunctionType == DdbUserDefinedFunctionType.Table ||
                                u.FunctionType == DdbUserDefinedFunctionType.Inline)
                                .ToList();
                        var scalarValued = functions.Where(u => u.FunctionType == DdbUserDefinedFunctionType.Scalar).ToList();

                        AddNamedItemSequence(emitter, "Table-valued Functions", tableValued);
                        AddNamedItemSequence(emitter, "Scalar-valued Functions", scalarValued);
                        AddNamedItemSequence(emitter, "Aggregate Functions", objects.OfType<DbdUserDefinedAggregate>());

                        emitter.Emit(SequenceEnd());
                    });

                    AddNamedItemSequence(emitter, "Assemblies", objects.OfType<DdbAssembly>());

                    NamedComplexSequenceEntry(emitter, "Types", emitter =>
                    {
                        emitter.Emit(new Scalar("items"));
                        emitter.Emit(SequenceStart());

                        AddNamedItemSequence(emitter, "User-Defined Data Types", objects.OfType<DdbUserDefinedDataType>());
                        //AddNamedItemSequence(emitter, "User-Defined Table Types", objects.OfType<DdbUserDefinedTableType>());
                        AddNamedItemSequence(emitter, "User-Defined Types", objects.OfType<DdbUserDefinedType>());
                        AddNamedItemSequence(emitter, "XML Schema Collections", objects.OfType<DdbXmlSchemaCollection>());

                        emitter.Emit(SequenceEnd());
                    });

                    AddNamedItemSequence(emitter, "Rules", objects.OfType<DdbRule>());
                    AddNamedItemSequence(emitter, "Defaults", objects.OfType<DdbDefault>());
                    AddNamedItemSequence(emitter, "Sequences", objects.OfType<DdbSequence>());

                    emitter.Emit(SequenceEnd());
                });


                NamedComplexSequenceEntry(emitter, "Security", emitter =>
                {
                    emitter.Emit(new Scalar("items"));
                    emitter.Emit(SequenceStart());

                    AddNamedItemSequence(emitter, "Users", objects.OfType<DdbUser>());
                    AddNamedItemSequence(emitter, "Schemas", objects.OfType<DdbSchema>());

                    NamedComplexSequenceEntry(emitter, "Roles", emitter =>
                    {
                        emitter.Emit(new Scalar("items"));
                        emitter.Emit(SequenceStart());

                        AddNamedItemSequence(emitter, "Database Roles", objects.OfType<DdbDatabaseRole>());
                        AddNamedItemSequence(emitter, "Application Roles", objects.OfType<DdbApplicationRole>());

                        emitter.Emit(SequenceEnd());
                    });

                    emitter.Emit(SequenceEnd());
                });

                emitter.Emit(SequenceEnd());
            });

            emitter.Emit(SequenceEnd());
            emitter.Emit(MappingEnd());

            emitter.Emit(new DocumentEnd(false));
            emitter.Emit(new StreamEnd());
        }

        static void NamedComplexSequenceEntry(IEmitter emitter, string name, Action<IEmitter> content)
        {
            emitter.Emit(MappingStart());
            emitter.Emit(new Scalar("name"));
            emitter.Emit(new Scalar(name));

            content(emitter);

            emitter.Emit(MappingEnd());
        }

        static void AddNamedItemSequence<T>(IEmitter emitter, string name, IEnumerable<T>? items) where T : DdbObject
        {
            NamedComplexSequenceEntry(emitter, name, e => AddItemSequence(items, e));
        }

        static void AddItemSequence<T>(IEnumerable<T>? items, IEmitter emitter) where T : DdbObject
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
                    emitter.Emit(new Scalar("type"));
                    emitter.Emit(new Scalar(item.Type));
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