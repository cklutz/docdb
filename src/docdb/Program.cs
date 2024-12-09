using DocDB.Model;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
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
            var modelCreator = new ModelCreator();
            var objects = new ConcurrentDictionary<string, List<DdbObject>>(StringComparer.OrdinalIgnoreCase);

            var serializer = new SerializerBuilder()
                .WithAttributeOverride<DdbObject>(o => o.Id, new YamlMemberAttribute { Order = -10 })
                .WithAttributeOverride<DdbObject>(o => o.Description!, new YamlMemberAttribute { Order = -9 })
                .WithAttributeOverride<DdbObject>(o => o.CreatedAt!, new YamlMemberAttribute { Order = -8 })
                .WithAttributeOverride<DdbObject>(o => o.LastModifiedAt!, new YamlMemberAttribute { Order = -7 })
                .WithAttributeOverride<NamedDdbObject>(o => o.Name, new YamlMemberAttribute { Order = -6 })
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

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

                var yaml = serializer.Serialize(dbdObject);

                string modelId = smoObject.GetModelId();
                string fullPath = Path.Combine(outputDirectory, modelId + ".yml");
                output.Message($"Writing {fullPath}");

                WriteFile(fullPath, urn.Type, yaml);
                objects.GetOrAdd(dbdObject.GetType().Name, _ => new()).Add(dbdObject);
            }

            string tocFile = Path.Combine(outputDirectory, "toc.yml");
            output.Message($"Writing {tocFile}");
            TocWriter.WriteToc(tocFile, objects);
        }

        return 0;
    }

    static void WriteFile(string name, string mimeType, string? contents)
    {
        if (File.Exists(name))
        {
            File.Delete(name);
        }

        File.WriteAllText(name, $"#YamlMime:DocDB_{mimeType}\r\n");
        File.AppendAllText(name, contents);
    }
}
