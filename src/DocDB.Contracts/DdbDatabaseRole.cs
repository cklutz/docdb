using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbDatabaseRole : NamedDdbObject
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("members"), JsonProperty("members")]
    public List<string> Members { get; set; } = [];
}
