using DocDB.Contracts;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DocDB;

public static class Program
{

    public static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: {0} CONNECTION-STRING OUTPUT-DIR ROOTNODE-TEXT");
            return 1;
        }

        var output = new Output();

        try
        {
            string connectionString = args[0];
            string outputDirectory = args[1];
            string rootNodeText = args[2];


            return DocumentDatabase(output, connectionString, outputDirectory, rootNodeText);
        }
        catch (Exception ex)
        {
            output.Error(ex.ToString());
            return ex.HResult;
        }
    }

    private static int DocumentDatabase(IOutput output, string connectionString, string outputDirectory, string rootNodeText)
    {
        if (!Directory.Exists(outputDirectory))
        {
            output.Debug($"Creating directory {outputDirectory}");
            Directory.CreateDirectory(outputDirectory);
        }

        using (var target = new SqlServerTarget(connectionString))
        {
            var modelCreator = new ModelCreator(target.Database);
            var objects = new List<DdbObject>();

            var serializerBuilder = new SerializerBuilder()
                .WithAttributeOverride<DdbObject>(o => o.Id, new YamlMemberAttribute { Order = -10 })
                .WithAttributeOverride<DdbObject>(o => o.Type, new YamlMemberAttribute { Order = -11 /*-9*/ })
                .WithAttributeOverride<DdbObject>(o => o.Description!, new YamlMemberAttribute { Order = -8 })
                .WithAttributeOverride<DdbObject>(o => o.CreatedAt!, new YamlMemberAttribute { Order = -7 })
                .WithAttributeOverride<DdbObject>(o => o.LastModifiedAt!, new YamlMemberAttribute { Order = -6 })
                .WithAttributeOverride<DdbObject>(o => o.Script!, new YamlMemberAttribute { Order = int.MaxValue - 10, ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal })
                .WithAttributeOverride<DdbStoredProcedure>(o => o.Syntax!, new YamlMemberAttribute { ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal })
                .WithAttributeOverride<DdbUserDefinedFunction>(o => o.Syntax!, new YamlMemberAttribute { ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal })
                .WithAttributeOverride<DdbXmlSchemaCollection>(o => o.Schemas!, new YamlMemberAttribute { ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal })
                .WithAttributeOverride<DdbXmlSchemaCollection>(o => o.SchemasError!, new YamlMemberAttribute { ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal })
                .WithAttributeOverride<NamedDdbObject>(o => o.Name, new YamlMemberAttribute { Order = -5 })
                .WithNamingConvention(CamelCaseNamingConvention.Instance);

            var serializer = serializerBuilder.Build();

            foreach (Urn urn in target.Objects)
            {
                output.Message($"Processing {urn}");

                var smoObject = target.GetSmoObject(urn);
                var dbdObject = modelCreator.CreateObject(smoObject);
                if (dbdObject == null)
                {
                    output.Warning($"Failed to create model for {urn.Value}");
                    continue;
                }

                var yaml = serializer.Serialize(dbdObject, dbdObject.GetType());

                string modelId = smoObject.GetModelId();
                string fullPath = Path.Combine(outputDirectory, modelId + ".yml");
                output.Message($"Writing {fullPath}");

                WriteFile(fullPath, modelId, yaml);
                objects.Add(dbdObject);
            }

            string tocFile = Path.Combine(outputDirectory, "toc.yml");
            output.Message($"Writing {tocFile}");
            TocWriter.WriteToc(tocFile, objects);
        }

        return 0;
    }

    static void WriteFile(string name, string uid, string? contents)
    {
        if (File.Exists(name))
        {
            File.Delete(name);
        }

        File.WriteAllText(name, $"### YamlMime:DocDB\r\n");
        File.AppendAllText(name, contents);
    }
}
