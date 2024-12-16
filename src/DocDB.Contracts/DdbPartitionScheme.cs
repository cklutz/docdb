using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbPartitionScheme : NamedDdbObject
{
    [JsonPropertyName("fileGroups"), JsonProperty("fileGroups")]
    public List<NamedDdbRef?> FileGroups { get; set; } = [];

    [JsonPropertyName("nextUsedFileGroup"), JsonProperty("nextUsedFileGroup")]
    public NamedDdbRef? NextUsedFileGroup { get; set; }

    [JsonPropertyName("partitionFunction"), JsonProperty("partitionFunction")]
    public NamedDdbRef? PartitionFunction { get; set; }
}
