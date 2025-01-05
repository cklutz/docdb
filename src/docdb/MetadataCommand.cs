using DocDB.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Spectre.Console.Cli;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ValidationResult = Spectre.Console.ValidationResult;

namespace DocDB;

internal class MetadataCommand : Command<MetadataCommandOptions>
{
    public override ValidationResult Validate(CommandContext context, MetadataCommandOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidationResult.Error("A connection string is required");
        }
        else
        {
            string? env = Environment.GetEnvironmentVariable(options.ConnectionString);
            if (env != null)
            {
                options.ConnectionString = env;
            }
            else if (File.Exists(options.ConnectionString))
            {
                options.ConnectionString = File.ReadAllText(options.ConnectionString).Trim();
            }

            try
            {
                _ = new SqlConnectionStringBuilder(options.ConnectionString);
            }
            catch (Exception ex)
            {
                return ValidationResult.Error($"The connection string is invalid: {ex.Message}");
            }
        }

        return base.Validate(context, options);
    }

    public override int Execute(CommandContext context, MetadataCommandOptions options)
    {
        return CommandHelper.Run(options, output =>
        {
            options.OutputFolder = Path.GetFullPath(options.OutputFolder!);

            return DocumentDatabase(output, options.ConnectionString, options.OutputFolder!, options.DisplayDatabaseName, options.SchemaVersionQuery);
        });
    }

    private static int DocumentDatabase(IOutput output, string connectionString, string outputDirectory, string? overrideDatabaseName, string? schemaVersionQuery)
    {
        if (!Directory.Exists(outputDirectory))
        {
            output.Debug($"Creating directory {outputDirectory}");
            Directory.CreateDirectory(outputDirectory);
        }

        using (var target = new SqlServerTarget(connectionString))
        {
            string? schemaVersion = null;
            if (schemaVersionQuery != null)
            {
                schemaVersion = target.ExecuteScalar(schemaVersionQuery) as string;
            }

            var modelCreator = new ModelCreator(output, target.Database, overrideDatabaseName, schemaVersion);
            var objects = new List<DdbObject>();

            var serializerBuilder = new SerializerBuilder()
                .WithAttributeOverride<DdbObject>(o => o.Id, new YamlMemberAttribute { Order = -10 })
                .WithAttributeOverride<DdbObject>(o => o.Type, new YamlMemberAttribute { Order = -11 /*-9*/ })
                .WithAttributeOverride<DdbObject>(o => o.Description!, new YamlMemberAttribute { Order = -8 })
                .WithAttributeOverride<DdbObject>(o => o.Script!, new YamlMemberAttribute { Order = int.MaxValue - 10, ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal })
                .WithAttributeOverride<DdbStoredProcedure>(o => o.Syntax!, new YamlMemberAttribute { ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal })
                .WithAttributeOverride<DdbUserDefinedFunction>(o => o.Syntax!, new YamlMemberAttribute { ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal })
                .WithAttributeOverride<DdbXmlSchemaCollection>(o => o.Schemas!, new YamlMemberAttribute { ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal })
                .WithAttributeOverride<DdbXmlSchemaCollection>(o => o.SchemasError!, new YamlMemberAttribute { ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal })
                .WithAttributeOverride<DdbAssembly>(o => o.SourceCode!, new YamlMemberAttribute { ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal })
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
            TocWriter.WriteToc(output, modelCreator, tocFile, objects);
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
