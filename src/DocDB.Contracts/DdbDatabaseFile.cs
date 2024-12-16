using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

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
