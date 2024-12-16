using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

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
