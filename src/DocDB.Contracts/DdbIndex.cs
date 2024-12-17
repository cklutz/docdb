using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

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

    [JsonPropertyName("partitionInfo"), JsonProperty("partitionInfo")]
    public DdbPartitionInfo PartitionInfo { get; set; } = new();

    [JsonPropertyName("fileGroup"), JsonProperty("fileGroup")]
    public NamedDdbRef? FileGroup { get; set; }

    [JsonPropertyName("fileStreamGroup"), JsonProperty("fileStreamGroup")]
    public NamedDdbRef? FileStreamGroup { get; set; }

    [JsonPropertyName("options"), JsonProperty("options")]
    public List<DdbOptionCategory> Options { get; set; } = [];
}
