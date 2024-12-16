using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbSchema : NamedDdbObject
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string? Owner { get; set; }
}
