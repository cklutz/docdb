using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbDataFile : DdbDatabaseFile
{
    [JsonPropertyName("fileGroup"), JsonProperty("fileGroup")]
    public NamedDdbRef? FileGroup { get; set; }
}
