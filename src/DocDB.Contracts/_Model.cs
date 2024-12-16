using Newtonsoft.Json;
using System.Collections;
using System.Numerics;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public interface IDdbRef
{
    string Id { get; }
    string Type { get; }
}

public interface INamedDdbRef : IDdbRef
{
    string Name { get; }
}

public class DdbRef : IDdbRef
{
    [JsonPropertyName("id"), JsonProperty("id")]
    public string Id { get; set; } = null!;
    [JsonPropertyName("type"), JsonProperty("type")]
    public string Type { get; set; } = null!;
}

public class NamedDdbRef : DdbRef, INamedDdbRef
{
    [JsonPropertyName("name"), JsonProperty("name")]
    public string Name { get; set; } = null!;
}

public abstract class DdbObject : IDdbRef
{
    protected DdbObject()
    {
        Type = GetTypeTag(GetType());
    }

    [JsonPropertyName("databaseId"), JsonProperty("databaseId")]
    public string DatabaseId { get; set; } = null!;

    [JsonPropertyName("id"), JsonProperty("id")]
    public string Id { get; set; } = null!;
    [JsonPropertyName("type"), JsonProperty("type")]
    public string Type { get; }
    [JsonPropertyName("description"), JsonProperty("description")]
    public string? Description { get; set; }
    [JsonPropertyName("script"), JsonProperty("script")]
    public string? Script { get; set; }
    [JsonPropertyName("createdAt"), JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }
    [JsonPropertyName("lastModifiedAt"), JsonProperty("lastModifiedAt")]
    public DateTime LastModifiedAt { get; set; }

    public override bool Equals(object? obj) => obj is DdbObject dbo && dbo.Id == Id && dbo.Type == Type;
    public override int GetHashCode() => HashCode.Combine(Id, Type);
    public override string ToString() => Id;

    public static Dictionary<string, Type> GetTypeMappings()
    {
        return typeof(DdbObject).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && t.IsClass && t.IsAssignableTo(typeof(DdbObject)))
            .ToDictionary(GetTypeTag, t => t);
    }

    private static string GetTypeTag(Type type) => GetTypeTag(type, false);
    public static string GetTypeTag<T>(bool isRef = false) where T : DdbObject => GetTypeTag(typeof(T), isRef);
    public static string GetTypeTag(Type type, bool isRef)
    {
        var span = type.Name.AsSpan();
        if (span.StartsWith("Ddb"))
        {
            span = span.Slice(3);
        }

        if (isRef)
        {
            return string.Concat(span, "Ref");
        }

        return span.ToString();
    }

    public virtual Dictionary<string, string> GetAllIds(bool includeSelf = true)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var queue = new Queue<DdbObject>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            bool shouldContinue = false;
            if (includeSelf || !ReferenceEquals(this, current))
            {
                shouldContinue = result.TryAdd(current.Id, current is NamedDdbObject named ? named.Name : current.Type);
            }

            if (shouldContinue)
            {
                var properties = current.GetType()
                    .GetProperties()
                    .Where(p => typeof(DdbObject).IsAssignableFrom(p.PropertyType) ||
                           (p.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(p.PropertyType)));

                foreach (var property in properties)
                {
                    var value = property.GetValue(current);

                    if (value is DdbObject DdbObjectValue)
                    {
                        queue.Enqueue(DdbObjectValue);
                    }
                    else if (value is IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            if (item is DdbObject DdbObjectItem)
                            {
                                queue.Enqueue(DdbObjectItem);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
}

public abstract class NamedDdbObject : DdbObject, INamedDdbRef
{
    [JsonPropertyName("name"), JsonProperty("name")]
    public string Name { get; set; } = null!;
}

public enum DdbServerProductType
{
    Unknown,
    SqlServer,
}

public class DdbDatabase : NamedDdbObject
{
    [JsonPropertyName("productInformation"), JsonProperty("productInformation")]
    public string? ProductInformation { get; set; }

    [JsonPropertyName("productType"), JsonProperty("productType")]
    public DdbServerProductType ProductType { get; set; }

    [JsonPropertyName("collation"), JsonProperty("collation")]
    public string? Collation { get; set; }

    [JsonPropertyName("isCloudHosted"), JsonProperty("isCloudHosted")]
    public bool IsCloudHosted { get; set; }

    [JsonPropertyName("fileGroups"), JsonProperty("fileGroups")]
    public List<DdbFileGroup> FileGroups { get; set; } = [];

    [JsonPropertyName("dataFiles"), JsonProperty("dataFiles")]
    public List<DdbDataFile> DataFiles { get; set; } = [];

    [JsonPropertyName("logFiles"), JsonProperty("logFiles")]
    public List<DdbLogFile> LogFiles { get; set; } = [];

    [JsonPropertyName("options"), JsonProperty("options")]
    public List<DdbOptionCategory> Options { get; set; } = [];
}

public class DdbOptionCategory
{
    public DdbOptionCategory()
    {
    }

    public DdbOptionCategory(string name)
    {
        Name = name;
    }

    [JsonPropertyName("name"), JsonProperty("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("entries"), JsonProperty("entries")]
    public List<DdbOptionEntry> Entries { get; set; } = [];
}

public class DdbOptionEntry
{
    public DdbOptionEntry()
    {
    }

    public DdbOptionEntry(string name, string? value, string? type)
    {
        Name = name;
        Value = value;
        Type = type;
    }

    [JsonPropertyName("name"), JsonProperty("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("value"), JsonProperty("value")]
    public string? Value { get; set; }

    [JsonPropertyName("type"), JsonProperty("type")]
    public string? Type { get; set; }
}

public class DdbFileGroup : NamedDdbObject
{
    [JsonPropertyName("isDefault"), JsonProperty("isDefault")]
    public bool IsDefault { get; set; }
    [JsonPropertyName("isFileStream"), JsonProperty("isFileStream")]
    public bool IsFileStream { get; set; }
    [JsonPropertyName("fileGroupType"), JsonProperty("fileGroupType")]
    public string? FileGroupType { get; set; }

    [JsonPropertyName("files"), JsonProperty("files")]
    public List<NamedDdbRef> Files { get; set; } = [];
}

public class DdbDataFile : DdbDatabaseFile
{
    [JsonPropertyName("fileGroup"), JsonProperty("fileGroup")]
    public NamedDdbRef? FileGroup { get; set; }
}

public class DdbLogFile : DdbDatabaseFile
{
}

public abstract class DdbDatabaseFile : NamedDdbObject
{
    [JsonPropertyName("fileName"), JsonProperty("fileName")]
    public string? FileName { get; set; }
    [JsonPropertyName("growth"), JsonProperty("growth")]
    public double Growth { get; set; }
    [JsonPropertyName("growthType"), JsonProperty("growthType")]
    public DdbGrowthType GrowthType { get; set; }
    [JsonPropertyName("isOffline"), JsonProperty("isOffline")]
    public bool IsOffline { get; set; }
    [JsonPropertyName("isReadOnly"), JsonProperty("isReadOnly")]
    public bool IsReadOnly { get; set; }
    [JsonPropertyName("isReadOnlyMedia"), JsonProperty("isReadOnlyMedia")]
    public bool IsReadOnlyMedia { get; set; }
    [JsonPropertyName("isSparse"), JsonProperty("isSparse")]
    public bool IsSparse { get; set; }
    [JsonPropertyName("maxSize"), JsonProperty("maxSize")]
    public double? MaxSize { get; set; }
    [JsonPropertyName("size"), JsonProperty("size")]
    public double Size { get; set; }
}

public enum DdbGrowthType
{
    Unknown,
    Fixed,
    KiloByte,
    Percent
}

public class DdbSchema : NamedDdbObject
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }
}

public class DdbUser : NamedDdbObject
{
    [JsonPropertyName("userType"), JsonProperty("userType")]
    public string? UserType { get; set; }

    [JsonPropertyName("authenticationType"), JsonProperty("authenticationType")]
    public string? AuthenticationType { get; set; }

    [JsonPropertyName("hasDbAccess"), JsonProperty("hasDbAccess")]
    public bool HasDBAccess { get; set; }

    [JsonPropertyName("loginType"), JsonProperty("loginType")]
    public string? LoginType { get; set; }

    [JsonPropertyName("login"), JsonProperty("login")]
    public string? Login { get; set; }
}

public class DdbApplicationRole : NamedDdbObject
{
    [JsonPropertyName("defaultSchema"), JsonProperty("defaultSchema")]
    public string? DefaultSchema { get; set; }
}

public class DdbDatabaseRole : NamedDdbObject
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("members"), JsonProperty("members")]
    public List<string> Members { get; set; } = [];
}

public enum DdbUserDefinedFunctionType
{
    Unknown,
    Scalar,
    Table,
    Inline
}

public class DdbDatabaseDdlTrigger : NamedDdbObject
{
    [JsonPropertyName("ansiNulls"), JsonProperty("ansiNulls")]
    public bool AnsiNulls { get; set; }
    [JsonPropertyName("quotedIdentifier"), JsonProperty("quotedIdentifier")]
    public bool QuotedIdentifier { get; set; }
    [JsonPropertyName("implementationType"), JsonProperty("implementationType")]
    public string? ImplementationType { get; set; }
    [JsonPropertyName("isEnabled"), JsonProperty("isEnabled")]
    public bool IsEnabled { get; set; }
    [JsonPropertyName("isEncrypted"), JsonProperty("isEncrypted")]
    public bool IsEncrypted { get; set; }
    [JsonPropertyName("notForReplication"), JsonProperty("notForReplication")]
    public bool NotForReplication { get; set; }
    [JsonPropertyName("text"), JsonProperty("text")]
    public string? Text { get; set; }

    [JsonPropertyName("assemblyName"), JsonProperty("assemblyName")]
    public string? AssemblyName { get; set; }
    [JsonPropertyName("assemblyRef"), JsonProperty("assemblyRef")]
    public NamedDdbRef? AssemblyRef { get; set; }
    [JsonPropertyName("className"), JsonProperty("className")]
    public string? ClassName { get; set; }
    [JsonPropertyName("methodName"), JsonProperty("methodName")]
    public string? MethodName { get; set; }
}

public abstract class DdbParameterBase : NamedDdbObject
{
    [JsonPropertyName("dataType"), JsonProperty("dataType")]
    public string? DataType { get; set; }
    [JsonPropertyName("dataTypeRef"), JsonProperty("dataTypeRef")]
    public NamedDdbRef? DataTypeRef { get; set; }
    [JsonPropertyName("defaultValue"), JsonProperty("defaultValue")]
    public string? DefaultValue { get; set; }

    public override bool Equals(object? obj) => obj is DdbColumnBase dbo && dbo.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => Name;
}

public class DdbStoredProcedureParameter : DdbParameterBase
{
    [JsonPropertyName("isOutputParameter"), JsonProperty("isOutputParameter")]
    public bool IsOutputParameter { get; set; }
    [JsonPropertyName("isCursorParameter"), JsonProperty("isCursorParameter")]
    public bool IsCursorParameter { get; set; }
}

public class DdbUserDefinedFunctionParameter : DdbParameterBase
{
}

public class DdbUserDefinedAggregateParameter : DdbParameterBase
{
}

public abstract class DdbProcedureOrFunctionObject<TParameter> : NamedDdbObject
    where TParameter : DdbParameterBase
{
    [JsonPropertyName("parameters"), JsonProperty("parameters")]
    public List<TParameter> Parameters { get; set; } = [];

    [JsonPropertyName("syntax"), JsonProperty("syntax")]
    public string? Syntax { get; set; }
}

public class DdbStoredProcedure : DdbProcedureOrFunctionObject<DdbStoredProcedureParameter>
{
}

public class DdbUserDefinedAggregate : DdbProcedureOrFunctionObject<DdbUserDefinedAggregateParameter>
{
    [JsonPropertyName("assemblyName"), JsonProperty("assemblyName")]
    public string? AssemblyName { get; set; }
    [JsonPropertyName("assemblyRef"), JsonProperty("assemblyRef")]
    public NamedDdbRef? AssemblyRef { get; set; }
    [JsonPropertyName("className"), JsonProperty("className")]
    public string? ClassName { get; set; }
    [JsonPropertyName("isSchemaOwned"), JsonProperty("isSchemaOwned")]
    public bool IsSchemaOwned { get; set; }
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("dataType"), JsonProperty("dataType")]
    public string? ReturnDataType { get; set; }
    [JsonPropertyName("dataTypeRef"), JsonProperty("dataTypeRef")]
    public NamedDdbRef? ReturnDataTypeRef { get; set; }
}

public class DdbUserDefinedFunction : DdbProcedureOrFunctionObject<DdbUserDefinedFunctionParameter>
{
    [JsonPropertyName("functionType"), JsonProperty("functionType")]
    public DdbUserDefinedFunctionType FunctionType { get; set; }
    [JsonPropertyName("returnsNullOnNullInput"), JsonProperty("returnsNullOnNullInput")]
    public bool ReturnsNullOnNullInput { get; set; }
}

public abstract class DdbColumnBase : NamedDdbObject
{
    [JsonPropertyName("dataType"), JsonProperty("dataType")]
    public string? DataType { get; set; }
    [JsonPropertyName("dataTypeRef"), JsonProperty("dataTypeRef")]
    public NamedDdbRef? DataTypeRef { get; set; }
    [JsonPropertyName("maxLengthBytes"), JsonProperty("maxLengthBytes")]
    public int? MaxLengthBytes { get; set; }
    [JsonPropertyName("precision"), JsonProperty("precision")]
    public int? Precision { get; set; }
    [JsonPropertyName("scale"), JsonProperty("scale")]
    public int? Scale { get; set; }

    [JsonPropertyName("isNullable"), JsonProperty("isNullable")]
    public bool IsNullable { get; set; }
    [JsonPropertyName("isComputed"), JsonProperty("isComputed")]
    public bool IsComputed { get; set; }
    [JsonPropertyName("computedText"), JsonProperty("computedText")]
    public string? ComputedText { get; set; }
    [JsonPropertyName("isIdentity"), JsonProperty("isIdentity")]
    public bool IsIdentity { get; set; }
    [JsonPropertyName("identityIncrement"), JsonProperty("identityIncrement")]
    public long? IdentityIncrement { get; set; }
    [JsonPropertyName("identitySeed"), JsonProperty("identitySeed")]
    public long? IdentitySeed { get; set; }
    [JsonPropertyName("inPrimaryKey"), JsonProperty("inPrimaryKey")]
    public bool InPrimaryKey { get; set; }
    [JsonPropertyName("isForeignKey"), JsonProperty("isForeignKey")]
    public bool IsForeignKey { get; set; }
    [JsonPropertyName("foreignKeys"), JsonProperty("foreignKeys")]
    public List<NamedDdbRef> ForeignKeys { get; set; } = [];
    [JsonPropertyName("default"), JsonProperty("default")]
    public string? Default { get; set; }
    [JsonPropertyName("collation"), JsonProperty("collation")]
    public string? Collation { get; set; }
    [JsonPropertyName("isFileStream"), JsonProperty("isFileStream")]
    public bool IsFileStream { get; set; }
    [JsonPropertyName("isFullTextIndexed"), JsonProperty("isFullTextIndexed")]
    public bool IsFullTextIndexed { get; set; }

    public override bool Equals(object? obj) => obj is DdbColumnBase dbo && dbo.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => Name;
}

public class DdbForeignKeyReference : NamedDdbObject
{
    public string TableName { get; set; } = null!;
}

public class DdbTableColumn : DdbColumnBase
{
}

public class DdbViewColumn : DdbColumnBase
{
}

public class DdbIndexColumn : NamedDdbObject
{
    [JsonPropertyName("columnRef"), JsonProperty("columnRef")]
    public NamedDdbRef? ColumnRef { get; set; }

    [JsonPropertyName("isDescending"), JsonProperty("isDescending")]
    public bool IsDescending { get; set; }

    [JsonPropertyName("isIncluded"), JsonProperty("isIncluded")]
    public bool IsIncluded { get; set; }

    [JsonPropertyName("columnStoreOrderOrdinal"), JsonProperty("columnStoreOrderOrdinal")]
    public int ColumnStoreOrderOrdinal { get; set; }
}

public class DdbIndex : NamedDdbObject
{
    [JsonPropertyName("indexType"), JsonProperty("indexType")]
    public string? IndexType { get; set; }

    [JsonPropertyName("indexKeyType"), JsonProperty("indexKeyType")]
    public string? IndexKeyType { get; set; }

    [JsonPropertyName("isDisabled"), JsonProperty("isDisabled")]
    public bool IsDisabled { get; set; }

    [JsonPropertyName("isUnique"), JsonProperty("isUnique")]
    public bool IsUnique { get; set; }

    [JsonPropertyName("isClustered"), JsonProperty("isClustered")]
    public bool IsClustered { get; set; }

    [JsonPropertyName("filter"), JsonProperty("filter")]
    public string? Filter { get; set; }

    [JsonPropertyName("columns"), JsonProperty("columns")]
    public List<DdbIndexColumn> Columns { get; set; } = [];

    [JsonPropertyName("isPartitioned"), JsonProperty("isPartitioned")]
    public bool IsPartitioned { get; set; }

    [JsonPropertyName("fileGroup"), JsonProperty("fileGroup")]
    public NamedDdbRef? FileGroup { get; set; }

    [JsonPropertyName("fileStreamGroup"), JsonProperty("fileStreamGroup")]
    public NamedDdbRef? FileStreamGroup { get; set; }

    [JsonPropertyName("options"), JsonProperty("options")]
    public List<DdbOptionCategory> Options { get; set; } = [];
}

public class DdbForeignKey : NamedDdbObject
{
    [JsonPropertyName("columns"), JsonProperty("columns")]
    public List<NamedDdbRef> Columns { get; set; } = [];
    [JsonPropertyName("isChecked"), JsonProperty("isChecked")]
    public bool IsChecked { get; set; }
    [JsonPropertyName("referencedKey"), JsonProperty("referencedKey")]
    public string? ReferencedKey { get; set; }
    [JsonPropertyName("referencedTable"), JsonProperty("referencedTable")]
    public NamedDdbRef? ReferencedTable { get; set; }
    [JsonPropertyName("deleteAction"), JsonProperty("deleteAction")]
    public string? DeleteAction { get; set; }
    [JsonPropertyName("updateAction"), JsonProperty("updateAction")]
    public string? UpdateAction { get; set; }
}

public class DdbCheckConstraint : NamedDdbObject
{
    [JsonPropertyName("isChecked"), JsonProperty("isChecked")]
    public bool IsChecked { get; set; }
    [JsonPropertyName("isEnabled"), JsonProperty("isEnabled")]
    public bool IsEnabled { get; set; }
    [JsonPropertyName("constraintText"), JsonProperty("constraintText")]
    public string? ConstraintText { get; set; }
}

public class DdbDmlTrigger : NamedDdbObject
{
    [JsonPropertyName("isEnabled"), JsonProperty("isEnabled")]
    public bool IsEnabled { get; set; }
    [JsonPropertyName("isSchemaBound"), JsonProperty("isSchemaBound")]
    public bool IsSchemaBound { get; set; }
    [JsonPropertyName("isInsteadOf"), JsonProperty("isInsteadOf")]
    public bool IsInsteadOf { get; set; }
    [JsonPropertyName("onDelete"), JsonProperty("onDelete")]
    public bool OnDelete { get; set; }
    [JsonPropertyName("onUpdate"), JsonProperty("onUpdate")]
    public bool OnUpdate { get; set; }
    [JsonPropertyName("onInsert"), JsonProperty("onInsert")]
    public bool OnInsert { get; set; }
}

public class DdbTablePartitionInfo
{
    [JsonPropertyName("isPartitioned"), JsonProperty("isPartitioned")]
    public bool IsPartitioned { get; set; }
    [JsonPropertyName("columns"), JsonProperty("columns")]
    public List<NamedDdbRef> Columns { get; set; } = [];
    [JsonPropertyName("partitionScheme"), JsonProperty("partitionScheme")]
    public NamedDdbRef? PartitionScheme { get; set; }
}

public class DdbPartitionScheme : NamedDdbObject
{
    [JsonPropertyName("fileGroups"), JsonProperty("fileGroups")]
    public List<NamedDdbRef?> FileGroups { get; set; } = [];

    [JsonPropertyName("nextUsedFileGroup"), JsonProperty("nextUsedFileGroup")]
    public NamedDdbRef? NextUsedFileGroup { get; set; }

    [JsonPropertyName("partitionFunction"), JsonProperty("partitionFunction")]
    public NamedDdbRef? PartitionFunction { get; set; }
}

public class DdbPartitionFunction : NamedDdbObject
{
    [JsonPropertyName("numberOfPartitions"), JsonProperty("numberOfPartitions")]
    public int NumberOfPartitions { get; set; }
    [JsonPropertyName("rangeType"), JsonProperty("rangeType")]
    public string? RangeType { get; set; }
    [JsonPropertyName("rangeValues"), JsonProperty("rangeValues")]
    public List<string> RangeValues { get; set; } = [];
    [JsonPropertyName("parameters"), JsonProperty("parameters")]
    public List<DdbPartitionFunctionParameter> Parameters { get; set; } = [];
}

public class DdbPartitionFunctionParameter : NamedDdbObject
{
    [JsonPropertyName("collation"), JsonProperty("collation")]
    public string? Collation { get; set; }
    [JsonPropertyName("length"), JsonProperty("length")]
    public int? Length { get; set; }
    [JsonPropertyName("numericPrecision"), JsonProperty("numericPrecision")]
    public int NumericPrecision { get; set; }
    [JsonPropertyName("numericScale"), JsonProperty("numericScale")]
    public int NumericScale { get; set; }
}

public abstract class TabularDdbObject<TColumn> : NamedDdbObject where TColumn : DdbColumnBase
{
    [JsonPropertyName("indexes"), JsonProperty("indexes")]
    public List<DdbIndex> Indexes { get; set; } = [];
    [JsonPropertyName("triggers"), JsonProperty("triggers")]
    public List<DdbDmlTrigger> Triggers { get; set; } = [];
    [JsonPropertyName("columns"), JsonProperty("columns")]
    public List<TColumn> Columns { get; set; } = [];
}

public class DdbTable : TabularDdbObject<DdbTableColumn>
{
    [JsonPropertyName("foreignKeys"), JsonProperty("foreignKeys")]
    public List<DdbForeignKey> ForeignKeys { get; set; } = [];
    [JsonPropertyName("checks"), JsonProperty("checks")]
    public List<DdbCheckConstraint> Checks { get; set; } = [];

    [JsonPropertyName("fileGroup"), JsonProperty("fileGroup")]
    public NamedDdbRef? FileGroup { get; set; }
    [JsonPropertyName("fileStreamGroup"), JsonProperty("fileStreamGroup")]
    public NamedDdbRef? FileStreamGroup { get; set; }
    [JsonPropertyName("textFileGroup"), JsonProperty("textFileGroup")]
    public NamedDdbRef? TextFileGroup { get; set; }

    [JsonPropertyName("partitionInfo"), JsonProperty("partitionInfo")]
    public DdbTablePartitionInfo PartitionInfo { get; set; } = new();
    [JsonPropertyName("fileTableInfo"), JsonProperty("fileTableInfo")]
    public DdbFileTableInfo FileTableInfo { get; set; } = new();
}

public class DdbFileTableInfo
{
    [JsonPropertyName("isFileTable"), JsonProperty("isFileTable")]
    public bool IsFileTable { get; set; }
    [JsonPropertyName("directoryName"), JsonProperty("directoryName")]
    public string? DirectoryName { get; set; }
    [JsonPropertyName("nameColumnCollation"), JsonProperty("nameColumnCollation")]
    public string? NameColumnCollation { get; set; }
    [JsonPropertyName("namespaceEnabled"), JsonProperty("namespaceEnabled")]
    public bool NamespaceEnabled { get; set; }
}

public class DdbView : TabularDdbObject<DdbViewColumn>
{
}

public class DdbSequence : NamedDdbObject
{
    [JsonPropertyName("dataType"), JsonProperty("dataType")]
    public string? DataType { get; set; }
    [JsonPropertyName("dataTypeRef"), JsonProperty("dataTypeRef")]
    public NamedDdbRef? DataTypeRef { get; set; }
    [JsonPropertyName("isCached"), JsonProperty("isCached")]
    public bool IsCached { get; set; }
    [JsonPropertyName("isCycleEnabled"), JsonProperty("isCycleEnabled")]
    public bool IsCycleEnabled { get; set; }
    [JsonPropertyName("isSchemaOwned"), JsonProperty("isSchemaOwned")]
    public bool IsSchemaOwned { get; set; }
    [JsonPropertyName("cacheSize"), JsonProperty("cacheSize")]
    public int? CacheSize { get; set; }
    [JsonPropertyName("incrementValue"), JsonProperty("incrementValue")]
    public string? IncrementValue { get; set; }
    [JsonPropertyName("startValue"), JsonProperty("startValue")]
    public string? StartValue { get; set; }
    [JsonPropertyName("minValue"), JsonProperty("minValue")]
    public string? MinValue { get; set; }
    [JsonPropertyName("maxValue"), JsonProperty("maxValue")]
    public string? MaxValue { get; set; }
}

public class DdbRule : NamedDdbObject
{
    [JsonPropertyName("textBody"), JsonProperty("textBody")]
    public string? TextBody { get; set; }
}

public class DdbDefault : NamedDdbObject
{
    [JsonPropertyName("textBody"), JsonProperty("textBody")]
    public string? TextBody { get; set; }
}

public class DdbUserDefinedType : NamedDdbObject
{
    [JsonPropertyName("assemblyName"), JsonProperty("assemblyName")]
    public string? AssemblyName { get; set; }
    [JsonPropertyName("assemblyRef"), JsonProperty("assemblyRef")]
    public NamedDdbRef? AssemblyRef { get; set; }
    [JsonPropertyName("className"), JsonProperty("className")]
    public string? ClassName { get; set; }
    [JsonPropertyName("userDefinedTypeFormat"), JsonProperty("userDefinedTypeFormat")]
    public string? UserDefinedTypeFormat { get; set; }
    [JsonPropertyName("binaryTypeIdentifier"), JsonProperty("binaryTypeIdentifier")]
    public string? BinaryTypeIdentifier { get; set; }
    [JsonPropertyName("isComVisible"), JsonProperty("isComVisible")]
    public bool IsComVisible { get; set; }
    [JsonPropertyName("isBinaryOrdered"), JsonProperty("isBinaryOrdered")]
    public bool IsBinaryOrdered { get; set; }
    [JsonPropertyName("isFixedLength"), JsonProperty("isFixedLength")]
    public bool IsFixedLength { get; set; }
    [JsonPropertyName("collation"), JsonProperty("collation")]
    public string? Collation { get; set; }
    [JsonPropertyName("isSchemaOwned"), JsonProperty("isSchemaOwned")]
    public bool IsSchemaOwned { get; set; }
    [JsonPropertyName("maxLength"), JsonProperty("maxLength")]
    public int? MaxLength { get; set; }
    [JsonPropertyName("isNullable"), JsonProperty("isNullable")]
    public bool IsNullable { get; set; }
    [JsonPropertyName("numericPrecision"), JsonProperty("numericPrecision")]
    public int NumericPrecision { get; set; }
    [JsonPropertyName("numericScale"), JsonProperty("numericScale")]
    public int NumericScale { get; set; }
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }
}

public class DdbAssembly : NamedDdbObject
{
    [JsonPropertyName("assemblyName"), JsonProperty("assemblyName")]
    public string? AssemblyName { get; set; }
    [JsonPropertyName("assemblySecurityLevel"), JsonProperty("assemblySecurityLevel")]
    public string? AssemblySecurityLevel { get; set; }
    [JsonPropertyName("culture"), JsonProperty("culture")]
    public string? Culture { get; set; }
    [JsonPropertyName("isVisible"), JsonProperty("isVisible")]
    public bool IsVisible { get; set; }
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }
    [JsonPropertyName("publicKey"), JsonProperty("publicKey")]
    public string? PublicKey { get; set; }
    [JsonPropertyName("version"), JsonProperty("version")]
    public string? Version { get; set; }
    [JsonPropertyName("fileNames"), JsonProperty("fileNames")]
    public List<string> FileNames { get; set; } = [];
}

public class DdbUserDefinedDataType : NamedDdbObject
{
    [JsonPropertyName("allowIdentity"), JsonProperty("allowIdentity")]
    public bool AllowIdentity { get; set; }
    [JsonPropertyName("collation"), JsonProperty("collation")]
    public string? Collation { get; set; }
    [JsonPropertyName("default"), JsonProperty("defaultRef")]
    public NamedDdbRef? DefaultRef { get; set; }
    [JsonPropertyName("isSchemaOwned"), JsonProperty("isSchemaOwned")]
    public bool IsSchemaOwned { get; set; }
    [JsonPropertyName("length"), JsonProperty("length")]
    public int? Length { get; set; }
    [JsonPropertyName("maxLength"), JsonProperty("maxLength")]
    public int? MaxLength { get; set; }
    [JsonPropertyName("isNullable"), JsonProperty("isNullable")]
    public bool IsNullable { get; set; }
    [JsonPropertyName("numericPrecision"), JsonProperty("numericPrecision")]
    public int NumericPrecision { get; set; }
    [JsonPropertyName("numericScale"), JsonProperty("numericScale")]
    public int NumericScale { get; set; }
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }
    [JsonPropertyName("rule"), JsonProperty("ruleRef")]
    public NamedDdbRef? RuleRef { get; set; }
    [JsonPropertyName("systemType"), JsonProperty("systemType")]
    public string? SystemType { get; set; }
    [JsonPropertyName("isVariableLength"), JsonProperty("isVariableLength")]
    public bool IsVariableLength { get; set; }
}

public class DdbUserDefinedTableType : TabularDdbObject<DdbTableColumn>
{
    [JsonPropertyName("collation"), JsonProperty("collation")]
    public string? Collation { get; set; }
    [JsonPropertyName("checks"), JsonProperty("checks")]
    public List<DdbCheckConstraint> Checks { get; set; } = [];

    [JsonPropertyName("isMemoryOptimized"), JsonProperty("isMemoryOptimized")]
    public bool IsMemoryOptimized { get; set; }
    [JsonPropertyName("isUserDefined"), JsonProperty("isUserDefined")]
    public bool IsUserDefined { get; set; }
    [JsonPropertyName("isSchemaOwned"), JsonProperty("isSchemaOwned")]
    public bool IsSchemaOwned { get; set; }
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }
    [JsonPropertyName("maxLength"), JsonProperty("maxLength")]
    public int? MaxLength { get; set; }
    [JsonPropertyName("isNullable"), JsonProperty("isNullable")]
    public bool IsNullable { get; set; }
}

public class DdbXmlSchemaCollection : NamedDdbObject
{
    [JsonPropertyName("schemas"), JsonProperty("schemas")]
    public List<DdbXmlSchema> Schemas { get; set; } = [];
    [JsonPropertyName("schemasError"), JsonProperty("schemasError")]
    public string? SchemasError { get; set; }
}

public class DdbXmlSchema
{
    [JsonPropertyName("id"), JsonProperty("id")]
    public string? Id { get; set; }
    [JsonPropertyName("namespace"), JsonProperty("namespace")]
    public string? Namespace { get; set; }
    [JsonPropertyName("text"), JsonProperty("text")]
    public string? Text { get; set; }
}