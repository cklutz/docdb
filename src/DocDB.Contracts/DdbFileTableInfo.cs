using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbFileTableInfo
{
    [JsonPropertyName("isFileTable"), JsonProperty("isFileTable")]
    public bool IsFileTable { get; set; }
    [JsonPropertyName("directoryName"), JsonProperty("directoryName")]
    public string? DirectoryName { get; set; }
    [JsonPropertyName("nameColumnCollation"), JsonProperty("nameColumnCollation")]
    public string? NameColumnCollation { get; set; }
    [JsonPropertyName("namespaceEnabled"), JsonProperty("namespaceEnabled")]
    public bool NamespaceEnabled { get; set; }
}
