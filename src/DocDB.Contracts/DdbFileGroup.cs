using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbFileGroup : NamedDdbObject
{
    [JsonPropertyName("isDefault"), JsonProperty("isDefault")]
    public bool IsDefault { get; set; }
    [JsonPropertyName("isFileStream"), JsonProperty("isFileStream")]
    public bool IsFileStream { get; set; }
    [JsonPropertyName("fileGroupType"), JsonProperty("fileGroupType")]
    public string? FileGroupType { get; set; }

    [JsonPropertyName("files"), JsonProperty("files")]
    public List<NamedDdbRef> Files { get; set; } = [];
}
