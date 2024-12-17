using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbTocSection : NamedDdbObject
{
    [JsonPropertyName("items"), JsonProperty("items")]
    public List<DdbTocSectionEntry> Items { get; set; } = [];
}
