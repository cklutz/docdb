using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public class DdbXmlSchema
{
    [JsonPropertyName("id"), JsonProperty("id")]
    public string? Id { get; set; }
    [JsonPropertyName("namespace"), JsonProperty("namespace")]
    public string? Namespace { get; set; }
    [JsonPropertyName("text"), JsonProperty("text")]
    public string? Text { get; set; }
}