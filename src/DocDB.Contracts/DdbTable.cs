using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbTable : TabularDdbObject<DdbTableColumn>
{
    [JsonPropertyName("foreignKeys"), JsonProperty("foreignKeys")]
    public List<DdbForeignKey> ForeignKeys { get; set; } = [];
    [JsonPropertyName("checks"), JsonProperty("checks")]
    public List<DdbCheckConstraint> Checks { get; set; } = [];

    [JsonPropertyName("fileGroup"), JsonProperty("fileGroup")]
    public NamedDdbRef? FileGroup { get; set; }
    [JsonPropertyName("fileStreamGroup"), JsonProperty("fileStreamGroup")]
    public NamedDdbRef? FileStreamGroup { get; set; }
    [JsonPropertyName("textFileGroup"), JsonProperty("textFileGroup")]
    public NamedDdbRef? TextFileGroup { get; set; }

    [JsonPropertyName("partitionInfo"), JsonProperty("partitionInfo")]
    public DdbTablePartitionInfo PartitionInfo { get; set; } = new();
    [JsonPropertyName("fileTableInfo"), JsonProperty("fileTableInfo")]
    public DdbFileTableInfo FileTableInfo { get; set; } = new();
}
