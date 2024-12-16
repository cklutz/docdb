using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

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
