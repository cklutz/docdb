using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbDefault : NamedDdbObject
{
    [JsonPropertyName("textBody"), JsonProperty("textBody")]
    public string? TextBody { get; set; }
}
