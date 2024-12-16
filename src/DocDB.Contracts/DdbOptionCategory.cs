using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbOptionCategory
{
    public DdbOptionCategory()
    {
    }

    public DdbOptionCategory(string name)
    {
        Name = name;
    }

    [JsonPropertyName("name"), JsonProperty("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("entries"), JsonProperty("entries")]
    public List<DdbOptionEntry> Entries { get; set; } = [];
}
