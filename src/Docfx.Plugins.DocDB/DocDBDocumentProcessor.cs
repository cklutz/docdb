using DocDB.Contracts;
using Docfx.Common;
using Docfx.YamlSerialization;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Composition;
using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeTypeResolvers;

namespace Docfx.Plugins.DocDB;

[Export(typeof(IDocumentProcessor))]
public class DocDBDocumentProcessor : IDocumentProcessor
{
    private const string DocumentTypeId = "DocDB";

    private readonly IDeserializer _deserializer;
    public DocDBDocumentProcessor()
    {
        Logger.LogVerbose($"{nameof(DocDBDocumentProcessor)}: created.");

        var builder = new DeserializerBuilder();
        builder.WithNamingConvention(CamelCaseNamingConvention.Instance);
        builder.IgnoreUnmatchedProperties();
        builder.WithTypeDiscriminatingNodeDeserializer(o =>
        {
            var mappings = DdbObject.GetTypeMappings();
            foreach (var x in mappings)
            {
                Logger.LogInfo("Mapping +++++ " + x.Key + " = " + x.Value);
            }
            o.AddKeyValueTypeDiscriminator<DdbObject>("type", mappings);
        });

        _deserializer = builder.Build();
    }

    public string Name => nameof(DocDBDocumentProcessor);

    public IEnumerable<IDocumentBuildStep> BuildSteps => [];

    public ProcessingPriority GetProcessingPriority(FileAndType file)
    {
        if (file.Type == DocumentType.Article && 
            ".yml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogVerbose($"{nameof(DocDBDocumentProcessor)}: {DocumentTypeId}: adding file {file.FullPath}.");
            return ProcessingPriority.Normal;
        }

        return ProcessingPriority.NotSupported;
    }

    public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
    {
        Logger.LogVerbose($"{nameof(DocDBDocumentProcessor)}: {DocumentTypeId}: loading file {file.FullPath}.");
        
        try
        {
            using var reader = new StreamReader(file.FullPath);
            var obj = _deserializer.Deserialize<DdbObject>(reader);

            var content = new Dictionary<string, object>(metadata.OrderBy(item => item.Key))
            {
                ["object"] = obj,
                ["type"] = DocumentTypeId,
                ["path"] = file.File,
                ["yamlmime"] = "DocDB",
            };

            var displayLocalPath = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, file.FullPath);

            return new FileModel(file, content)
            {
                LocalPathFromRoot = displayLocalPath,
            };
        }
        catch (Exception ex)
        {
            Logger.LogError($"{nameof(DocDBDocumentProcessor)}: ====> {ex}");
            throw;
        }
    }

    public SaveResult Save(FileModel model)
    {
        Logger.LogInfo($"{DocumentTypeId}: saving file {model.File}.");

        return new SaveResult
        {
            DocumentType = DocumentTypeId,
            
            FileWithoutExtension = Path.ChangeExtension(model.File, null),
        };
    }

    public void UpdateHref(FileModel model, IDocumentBuildContext context) { }
}
