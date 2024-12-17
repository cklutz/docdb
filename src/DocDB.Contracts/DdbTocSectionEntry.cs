using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbTocSectionEntry
{
    [JsonPropertyName("id"), JsonProperty("id")]
    public string Id { get; set; } = null!;
    [JsonPropertyName("name"), JsonProperty("name")]
    public string Name { get; set; } = null!;
    [JsonPropertyName("description"), JsonProperty("description")]
    public string Description { get; set; } = null!;
}
