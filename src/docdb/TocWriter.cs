using DocDB.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

using static YamlExtensions;

namespace DocDB
{
    internal static class TocWriter
    {
        // "Database"
        //      Tables
        //      Views
        //      Synonyms
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
        //          Partition Schemes
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

            emitter.EmitNamed(databaseObj.Name, emitter =>
            {
                var uidBuilder = new UidBuilder(databaseObj.Id, databaseObj.Name, new TocSectionWriter(databaseObj.Id, Path.GetDirectoryName(Path.GetFullPath(fileName))!));

                emitter.EmitNamedScalar("uid", uidBuilder.Value);
                emitter.EmitNamedScalar("type", databaseObj.Type);
                emitter.Emit(new Scalar("items"));
                emitter.Emit(SequenceStart());

                emitter.EmitNamedItemSequence("Tables", uidBuilder, objects.OfType<DdbTable>());
                emitter.EmitNamedItemSequence("Views", uidBuilder, objects.OfType<DdbView>());
                emitter.EmitNamedItemSequence("Synonyms", uidBuilder, objects.OfType<DdbSynonym>());

                emitter.EmitNamed("Programmability", emitter =>
                {
                    using var scope = uidBuilder.GetScope("progs", "Programmability");

                    emitter.EmitNamedScalar("uid", uidBuilder.Value);
                    emitter.Emit(new Scalar("items"));
                    emitter.Emit(SequenceStart());

                    emitter.EmitNamedItemSequence("Stored Procedures", uidBuilder, objects.OfType<DdbStoredProcedure>());

                    emitter.EmitNamed("Functions", emitter =>
                    {
                        using var scope = uidBuilder.GetScope("functions", "Functions");
                        emitter.EmitNamedScalar("uid", uidBuilder.Value);
                        emitter.Emit(new Scalar("items"));
                        emitter.Emit(SequenceStart());

                        var functions = objects.OfType<DdbUserDefinedFunction>();

                        // SSMS also treats "inline" as "table"
                        var tableValued = functions.Where(u =>
                                u.FunctionType == DdbUserDefinedFunctionType.Table ||
                                u.FunctionType == DdbUserDefinedFunctionType.Inline)
                                .ToList();
                        var scalarValued = functions.Where(u => u.FunctionType == DdbUserDefinedFunctionType.Scalar).ToList();

                        emitter.EmitNamedItemSequence("Table-valued Functions", uidBuilder, tableValued);
                        emitter.EmitNamedItemSequence("Scalar-valued Functions", uidBuilder, scalarValued);
                        emitter.EmitNamedItemSequence("Aggregate Functions", uidBuilder, objects.OfType<DdbUserDefinedAggregate>());

                        emitter.Emit(SequenceEnd());
                    });

                    emitter.EmitNamedItemSequence("Database Triggers", uidBuilder, objects.OfType<DdbDatabaseDdlTrigger>());
                    emitter.EmitNamedItemSequence("Assemblies", uidBuilder, objects.OfType<DdbAssembly>());

                    emitter.EmitNamed("Types", emitter =>
                    {
                        using var scope = uidBuilder.GetScope("types", "Types");

                        emitter.EmitNamedScalar("uid", uidBuilder.Value);
                        emitter.Emit(new Scalar("items"));
                        emitter.Emit(SequenceStart());

                        emitter.EmitNamedItemSequence("User-Defined Data Types", uidBuilder, objects.OfType<DdbUserDefinedDataType>());
                        emitter.EmitNamedItemSequence("User-Defined Table Types", uidBuilder, objects.OfType<DdbUserDefinedTableType>());
                        emitter.EmitNamedItemSequence("User-Defined Types", uidBuilder, objects.OfType<DdbUserDefinedType>());
                        emitter.EmitNamedItemSequence("XML Schema Collections", uidBuilder, objects.OfType<DdbXmlSchemaCollection>());

                        emitter.Emit(SequenceEnd());
                    });

                    emitter.EmitNamedItemSequence("Rules", uidBuilder, objects.OfType<DdbRule>());
                    emitter.EmitNamedItemSequence("Defaults", uidBuilder, objects.OfType<DdbDefault>());
                    emitter.EmitNamedItemSequence("Sequences", uidBuilder, objects.OfType<DdbSequence>());

                    emitter.Emit(SequenceEnd());
                });

                emitter.EmitNamed("Storage", emitter =>
                {
                    using var scope = uidBuilder.GetScope("storage", "Storage");
                    emitter.EmitNamedScalar("uid", uidBuilder.Value);
                    emitter.Emit(new Scalar("items"));
                    emitter.Emit(SequenceStart());

                    emitter.EmitNamedItemSequence("Partition Schemes ", uidBuilder, objects.OfType<DdbPartitionScheme>());
                    emitter.EmitNamedItemSequence("Partition Functions", uidBuilder, objects.OfType<DdbPartitionFunction>());

                    emitter.Emit(SequenceEnd());
                });

                emitter.EmitNamed("Security", emitter =>
                {
                    using var scope = uidBuilder.GetScope("security", "Security");
                    emitter.EmitNamedScalar("uid", uidBuilder.Value);
                    emitter.Emit(new Scalar("items"));
                    emitter.Emit(SequenceStart());

                    emitter.EmitNamedItemSequence("Users", uidBuilder, objects.OfType<DdbUser>());
                    emitter.EmitNamedItemSequence("Schemas", uidBuilder, objects.OfType<DdbSchema>());

                    emitter.EmitNamed("Roles", emitter =>
                    {
                        using var scope = uidBuilder.GetScope("roles", "Roles");
                        emitter.EmitNamedScalar("uid", uidBuilder.Value);
                        emitter.Emit(new Scalar("items"));
                        emitter.Emit(SequenceStart());

                        emitter.EmitNamedItemSequence("Database Roles", uidBuilder, objects.OfType<DdbDatabaseRole>());
                        emitter.EmitNamedItemSequence("Application Roles", uidBuilder, objects.OfType<DdbApplicationRole>());

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
    }
}
