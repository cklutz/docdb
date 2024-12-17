using DocDB.Contracts;
using Docfx.Build.Common;
using Docfx.Common;
using Docfx.Common.Git;
using Docfx.DataContracts.Common;
using System.Collections.Immutable;
using System.Composition;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Docfx.Plugins.DocDB;

[Export(typeof(IDocumentProcessor))]
public class DocDBDocumentProcessor : ReferenceDocumentProcessorBase
{
    private const string DocDBDocumentType = "DocDB";
    private const string DocumentTypeKey = "documentType";

    private readonly IDeserializer _deserializer;

    public DocDBDocumentProcessor()
    {
        Logger.LogVerbose($"{nameof(DocDBDocumentProcessor)}: created.");

        var builder = new DeserializerBuilder();
        builder.WithNamingConvention(CamelCaseNamingConvention.Instance);
        builder.IgnoreUnmatchedProperties();
        builder.WithTypeDiscriminatingNodeDeserializer(
            o => o.AddKeyValueTypeDiscriminator<DdbObject>("type", DdbObject.GetTypeMappings()));

        _deserializer = builder.Build();
    }

    protected override string ProcessedDocumentType => DocDBDocumentType;
    public override string Name => nameof(DocDBDocumentProcessor);
    public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; } = [];

    public override ProcessingPriority GetProcessingPriority(FileAndType file)
    {
        if (file.Type == DocumentType.Article &&
            ".yml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogVerbose($"{nameof(DocDBDocumentProcessor)}: {ProcessedDocumentType}: adding file {file.FullPath}.");
            return ProcessingPriority.Normal;
        }

        return ProcessingPriority.NotSupported;
    }

    protected override FileModel LoadArticle(FileAndType file, ImmutableDictionary<string, object> metadata)
    {
        Logger.LogVerbose($"{nameof(DocDBDocumentProcessor)}: {ProcessedDocumentType}: loading file {file.FullPath}.");

        using var reader = new StreamReader(file.FullPath);
        var obj = _deserializer.Deserialize<DdbObject>(reader);

        var vm = new DocDBViewModel(obj);
        vm.DisplayType = GetDisplayType(obj.Type);
        vm.Metadata = metadata.ToDictionary();
        vm.Metadata[DocumentTypeKey] = DocDBDocumentType;

        var repoInfo = GitUtility.TryGetFileDetail(file.FullPath);
        if (repoInfo != null)
        {
            vm.Metadata["source"] = new SourceDetail { Remote = repoInfo };
        }

        vm.Metadata["_lastSchemaModification"] = obj.LastSchemaModificationAt.ToString("g");

        var localPathFromRoot = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, file.FullPath);

        return new FileModel(file, vm)
        {
            Uids = [.. vm.Payload.GetAllIds().Select(id => new UidDefinition(id.Key, localPathFromRoot))],
            LocalPathFromRoot = localPathFromRoot
        };
    }

    public override SaveResult Save(FileModel model)
    {
        if (model.Type != DocumentType.Article)
        {
            throw new NotSupportedException();
        }

        Logger.LogVerbose($"{ProcessedDocumentType}: saving file {model.File}.");

        var vm = (DocDBViewModel)model.Content;
        if (vm.Metadata.TryGetValue(DocumentTypeKey, out object? documentTypeObject) &&
            documentTypeObject is string documentType)
        {
            model.DocumentType = documentType;
        }

        var result = base.Save(model);
        result.XRefSpecs = GetXRefInfo(vm.Payload, model.Key).ToImmutableArray();
        return result;
    }

    private static string GetDisplayType(string type) => OutputHelper.SplitPascalCase(type);

    private static IEnumerable<XRefSpec> GetXRefInfo(DdbObject obj, string key)
    {
        foreach ((var id, var name) in obj.GetAllIds())
        {
            yield return new XRefSpec
            {
                Uid = id,
                Name = name,
                Href = key,
            };
        }
    }

    public override void UpdateHref(FileModel model, IDocumentBuildContext context) { }
}
