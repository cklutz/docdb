using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbOptionEntry
{
    public DdbOptionEntry()
    {
    }

    public DdbOptionEntry(string name, string? value, string? type)
    {
        Name = name;
        Value = value;
        Type = type;
    }

    [JsonPropertyName("name"), JsonProperty("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("value"), JsonProperty("value")]
    public string? Value { get; set; }

    [JsonPropertyName("type"), JsonProperty("type")]
    public string? Type { get; set; }
}
