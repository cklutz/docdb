using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbTablePartitionInfo
{
    [JsonPropertyName("isPartitioned"), JsonProperty("isPartitioned")]
    public bool IsPartitioned { get; set; }
    [JsonPropertyName("columns"), JsonProperty("columns")]
    public List<NamedDdbRef> Columns { get; set; } = [];
    [JsonPropertyName("partitionScheme"), JsonProperty("partitionScheme")]
    public NamedDdbRef? PartitionScheme { get; set; }
}
