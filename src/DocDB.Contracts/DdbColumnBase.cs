using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

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
