using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbApplicationRole : NamedDdbObject
{
    [JsonPropertyName("defaultSchema"), JsonProperty("defaultSchema")]
    public string? DefaultSchema { get; set; }
}
