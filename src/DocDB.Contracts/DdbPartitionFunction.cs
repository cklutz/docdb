using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

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
