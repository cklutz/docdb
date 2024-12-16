using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbRef : IDdbRef
{
    [JsonPropertyName("id"), JsonProperty("id")]
    public string Id { get; set; } = null!;
    [JsonPropertyName("type"), JsonProperty("type")]
    public string Type { get; set; } = null!;
}
