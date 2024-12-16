using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class NamedDdbRef : DdbRef, INamedDdbRef
{
    [JsonPropertyName("name"), JsonProperty("name")]
    public string Name { get; set; } = null!;
}
