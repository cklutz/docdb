using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

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
    [JsonPropertyName("maxLength"), JsonProperty("maxLength")]
    public int? MaxLength { get; set; }
    [JsonPropertyName("isNullable"), JsonProperty("isNullable")]
    public bool IsNullable { get; set; }
    [JsonPropertyName("numericPrecision"), JsonProperty("numericPrecision")]
    public int NumericPrecision { get; set; }
    [JsonPropertyName("numericScale"), JsonProperty("numericScale")]
    public int NumericScale { get; set; }
    [JsonPropertyName("isSchemaOwned"), JsonProperty("isSchemaOwned")]
    public bool IsSchemaOwned { get; set; }
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }
}
