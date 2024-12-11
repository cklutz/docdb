using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public abstract class DdbObject
{
    protected DdbObject()
    {
        Type = GetTypeTag(GetType());
    }

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

    public static string GetTypeTag<T>() where T : DdbObject => GetTypeTag(typeof(T));

    private static string GetTypeTag(Type type)
    {
        var span = type.Name.AsSpan();
        if (span.StartsWith("Ddb"))
        {
            span = span.Slice(3);
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

public abstract class NamedDdbObject : DdbObject
{
    [JsonPropertyName("name"), JsonProperty("name")]
    public string Name { get; set; } = null!;
}

public class DdbSchema : NamedDdbObject
{
}

public enum DdbUserDefinedFunctionType
{
    Unknown,
    Scalar,
    Table,
    Inline
}


public abstract class DdbParameterBase : NamedDdbObject
{
    [JsonPropertyName("dataType"), JsonProperty("dataType")]
    public string? DataType { get; set; }
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

public abstract class DdbProcedureOrFunctionObject<TParameter> : NamedDdbObject
    where TParameter : DdbParameterBase
{
    [JsonPropertyName("parameters"), JsonProperty("parameters")]
    public List<TParameter> Parameters { get; set; } = [];
}

public class DdbStoredProcedure : DdbProcedureOrFunctionObject<DdbStoredProcedureParameter>
{
}

public class DbdUserDefinedAggregate : NamedDdbObject
{
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
    [JsonPropertyName("maxLengthBytes"), JsonProperty("maxLengthBytes")]
    public int? MaxLengthBytes { get; set; }
    [JsonPropertyName("precision"), JsonProperty("precision")]
    public int? Precision { get; set; }
    [JsonPropertyName("scale"), JsonProperty("scale")]
    public int? Scale { get; set; }

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
    [JsonPropertyName("default"), JsonProperty("default")]
    public string? Default { get; set; }

    public override bool Equals(object? obj) => obj is DdbColumnBase dbo && dbo.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => Name;
}

public class DdbTableColumn : DdbColumnBase
{
}

public class DdbViewColumn : DdbColumnBase
{
}

public class DdbIndex : NamedDdbObject
{
    [JsonPropertyName("columns"), JsonProperty("columns")]
    public List<string> Columns { get; set; } = [];
    [JsonPropertyName("isUnique"), JsonProperty("isUnique")]
    public bool IsUnique { get; set; }
    [JsonPropertyName("isClustered"), JsonProperty("isClustered")]
    public bool IsClustered { get; set; }
    [JsonPropertyName("isPartitioned"), JsonProperty("isPartitioned")]
    public bool IsPartitioned { get; set; }
}

public class DdbForeignKey : NamedDdbObject
{
    [JsonPropertyName("columns"), JsonProperty("columns")]
    public List<string> Columns { get; set; } = [];
    [JsonPropertyName("noCheck"), JsonProperty("noCheck")]
    public bool NoCheck { get; set; }
}

public class DdbCheckConstraint : NamedDdbObject
{
    [JsonPropertyName("on"), JsonProperty("on")]
    public string? On { get; set; }
    [JsonPropertyName("constraintText"), JsonProperty("constraintText")]
    public string? ConstraintText { get; set; }
}

public class DdbTablePartitionInfo
{
    [JsonPropertyName("isPartitioned"), JsonProperty("isPartitioned")]
    public bool IsPartitioned { get; set; }
    [JsonPropertyName("columns"), JsonProperty("columns")]
    public List<string> Columns { get; set; } = [];
    [JsonPropertyName("partitionScheme"), JsonProperty("partitionScheme")]
    public string? PartitionScheme { get; set; }
    [JsonPropertyName("fileGroup"), JsonProperty("fileGroup")]
    public string? FileGroup { get; set; }
}

public abstract class TabularDdbObject<TColumn> : NamedDdbObject where TColumn : DdbColumnBase
{
    [JsonPropertyName("indexes"), JsonProperty("indexes")]
    public List<DdbIndex> Indexes { get; set; } = [];
    [JsonPropertyName("columns"), JsonProperty("columns")]
    public List<TColumn> Columns { get; set;  } = [];
}

public class DdbTable : TabularDdbObject<DdbTableColumn>
{
    [JsonPropertyName("foreignKeys"), JsonProperty("foreignKeys")]
    public List<DdbForeignKey> ForeignKeys { get; set; } = [];
    [JsonPropertyName("checks"), JsonProperty("checks")]
    public List<DdbCheckConstraint> Checks { get; set; } = [];
    [JsonPropertyName("partitionInfo"), JsonProperty("partitionInfo")]
    public DdbTablePartitionInfo PartitionInfo { get; set; } = new();
}

public class DdbView : TabularDdbObject<DdbViewColumn>
{
}
