using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public abstract class NamedDdbObject : DdbObject, INamedDdbRef
{
    [JsonPropertyName("name"), JsonProperty("name")]
    public string Name { get; set; } = null!;
}
